using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GoldAI.App.Services;

/// <summary>
/// Orchestrates the daily execution pipeline:
/// 0. Fetch current prices from Nobitex API and store them.
/// 1. Evaluate prediction accuracy vs real prices; force retrain if below threshold.
/// 2. Load historical price data from the database.
/// 3. Train / refresh the ML model if necessary.
/// 4. Run the full analysis.
/// 5. Persist the analysis result.
/// 6. Print a summary to the console.
/// </summary>
public class DailyRunner
{
    private readonly IPriceRepository           _priceRepository;
    private readonly IAnalysisRepository        _analysisRepository;
    private readonly IModelTrainer              _modelTrainer;
    private readonly IAnalysisEngine            _analysisEngine;
    private readonly IModelPredictor            _modelPredictor;
    private readonly IPriceScraperService?      _priceScraper;
    private readonly PredictionAccuracyChecker? _accuracyChecker;
    private readonly ILogger<DailyRunner>       _logger;

    // Retrain the model after this many new price records have been added
    private const int RetrainEveryNDays = 7;

    public DailyRunner(
        IPriceRepository            priceRepository,
        IAnalysisRepository         analysisRepository,
        IModelTrainer               modelTrainer,
        IAnalysisEngine             analysisEngine,
        IModelPredictor             modelPredictor,
        ILogger<DailyRunner>        logger,
        IPriceScraperService?       priceScraper     = null,
        PredictionAccuracyChecker?  accuracyChecker  = null)
    {
        _priceRepository    = priceRepository;
        _analysisRepository = analysisRepository;
        _modelTrainer       = modelTrainer;
        _analysisEngine     = analysisEngine;
        _modelPredictor     = modelPredictor;
        _priceScraper       = priceScraper;
        _accuracyChecker    = accuracyChecker;
        _logger             = logger;
    }

    /// <summary>
    /// Runs the complete daily analysis pipeline.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("=== GoldAI Daily Runner — {Date} ===", DateTime.UtcNow.Date);

        // ── 0. Fetch and store current prices ─────────────────────────────────
        if (_priceScraper is not null)
        {
            _logger.LogInformation("Fetching current prices from Nobitex API...");
            try
            {
                var fetchedPrices = await _priceScraper.ScrapeCurrentPricesAsync(ct);
                await SaveScrapedPricesAsync(fetchedPrices, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch prices. Continuing with existing data.");
            }
        }

        // ── 1. Load data ──────────────────────────────────────────────────────
        _logger.LogInformation("Loading price data from database...");
        var goldPrices   = await _priceRepository.GetAllAsync(AssetType.Gold,   ct);
        var silverPrices = await _priceRepository.GetAllAsync(AssetType.Silver, ct);
        var usdPrices    = await _priceRepository.GetAllAsync(AssetType.USD,    ct);

        _logger.LogInformation(
            "Loaded {Gold} Gold, {Silver} Silver, {Usd} USD price records.",
            goldPrices.Count, silverPrices.Count, usdPrices.Count);

        int minRecords = Math.Min(goldPrices.Count, Math.Min(silverPrices.Count, usdPrices.Count));
        if (minRecords < 10)
        {
            _logger.LogWarning(
                "Insufficient price history (minimum 10 records per asset required). " +
                "Skipping analysis for today.");
            return;
        }

        // ── 2. Prediction accuracy check → may force retrain ──────────────────
        bool accuracyForcedRetrain = false;
        if (_accuracyChecker is not null)
        {
            try
            {
                accuracyForcedRetrain = await _accuracyChecker.ShouldForceRetrainAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Prediction accuracy check failed; proceeding normally.");
            }
        }

        // ── 3. Train / refresh model ──────────────────────────────────────────
        bool shouldRetrain = accuracyForcedRetrain
            || !_modelPredictor.IsModelAvailable()
            || ShouldRetrain(minRecords);

        if (shouldRetrain)
        {
            string reason = accuracyForcedRetrain          ? "low prediction accuracy"
                          : !_modelPredictor.IsModelAvailable() ? "no saved model"
                          : "periodic schedule";
            _logger.LogInformation("Training ML model ({Reason}) on {N} records...", reason, minRecords);
            await _modelTrainer.TrainAsync(goldPrices, silverPrices, usdPrices, ct);
            _logger.LogInformation("Model training complete.");
        }

        // ── 4. Run analysis ───────────────────────────────────────────────────
        _logger.LogInformation("Running daily analysis...");
        var result = await _analysisEngine.RunAsync(goldPrices, silverPrices, usdPrices, ct);

        // ── 5. Persist result ─────────────────────────────────────────────────
        await _analysisRepository.SaveAsync(result, ct);
        _logger.LogInformation("Analysis result saved.");

        // ── 6. Print summary ──────────────────────────────────────────────────
        PrintSummary(result);
    }

    private static bool ShouldRetrain(int totalRecords) =>
        totalRecords % RetrainEveryNDays == 0;

    private void PrintSummary(DailyAnalysisResult r)
    {
        _logger.LogInformation("──────────────────────────────────────────────────");
        _logger.LogInformation("DAILY ANALYSIS SUMMARY — {Date:yyyy-MM-dd}", r.Date);
        _logger.LogInformation("──────────────────────────────────────────────────");
        _logger.LogInformation("Prices    │ Gold: {Gold:F2}  Silver: {Silver:F2}  USD: {Usd:F4}",
            r.GoldPrice, r.SilverPrice, r.UsdPrice);

        if (r.GoldPrediction is not null)
        {
            _logger.LogInformation(
                "Gold ML   │ Up: {Up:P1}  Crash: {Crash:P1}  Trend: {Trend:P1}",
                r.GoldPrediction.UpProbability,
                r.GoldPrediction.CrashProbability,
                r.GoldPrediction.TrendContinuationProbability);
        }
        if (r.SilverPrediction is not null)
        {
            _logger.LogInformation(
                "Silver ML │ Up: {Up:P1}  Crash: {Crash:P1}  Trend: {Trend:P1}",
                r.SilverPrediction.UpProbability,
                r.SilverPrediction.CrashProbability,
                r.SilverPrediction.TrendContinuationProbability);
        }

        _logger.LogInformation(
            "Scores    │ Gold: {Gold:+0.00;-0.00}  Silver: {Silver:+0.00;-0.00}  USD: {Usd:+0.00;-0.00}",
            r.GoldScore, r.SilverScore, r.UsdScore);

        if (r.Allocation is not null)
        {
            _logger.LogInformation(
                "Allocation│ Gold: {Gold:P0}  Silver: {Silver:P0}  USD: {Usd:P0}",
                r.Allocation.GoldWeight, r.Allocation.SilverWeight, r.Allocation.UsdWeight);
        }

        _logger.LogInformation("Risk Score│ {Risk:F2}  {Crash}{Vol}",
            r.OverallRiskScore,
            r.CrashWarning ? "⚠ CRASH WARNING  " : "",
            r.HighVolatilityWarning ? "⚠ HIGH VOLATILITY" : "");
        _logger.LogInformation("──────────────────────────────────────────────────");
    }

    /// <summary>
    /// Saves fetched prices to the database, skipping duplicates (same Asset + Date).
    /// </summary>
    private async Task SaveScrapedPricesAsync(
        IReadOnlyList<AssetPrice> fetchedPrices,
        CancellationToken ct)
    {
        foreach (var price in fetchedPrices)
        {
            try
            {
                var existing = await _priceRepository.GetRangeAsync(
                    price.Asset, price.Date, price.Date, ct);

                if (existing.Count == 0)
                {
                    await _priceRepository.SaveAsync(price, ct);
                    _logger.LogInformation(
                        "New price saved: {Asset} on {Date} = {Price:N0} IRT",
                        price.Asset, price.Date, price.Close);
                }
                else
                {
                    _logger.LogDebug(
                        "Price already exists for {Asset} on {Date}, skipping.",
                        price.Asset, price.Date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to save price for {Asset} on {Date}",
                    price.Asset, price.Date);
            }
        }
    }
}
