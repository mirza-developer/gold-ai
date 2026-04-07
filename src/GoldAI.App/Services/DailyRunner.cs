using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GoldAI.App.Services;

/// <summary>
/// Orchestrates the daily execution pipeline:
/// 1. Load historical price data from the database.
/// 2. Train / refresh the ML model if necessary.
/// 3. Run the full analysis.
/// 4. Persist the analysis result.
/// 5. Print a summary to the console.
/// </summary>
public class DailyRunner
{
    private readonly IPriceRepository   _priceRepository;
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IModelTrainer       _modelTrainer;
    private readonly IAnalysisEngine     _analysisEngine;
    private readonly IModelPredictor     _modelPredictor;
    private readonly ILogger<DailyRunner> _logger;

    // Retrain the model after this many new price records have been added
    private const int RetrainEveryNDays = 7;

    public DailyRunner(
        IPriceRepository    priceRepository,
        IAnalysisRepository analysisRepository,
        IModelTrainer        modelTrainer,
        IAnalysisEngine      analysisEngine,
        IModelPredictor      modelPredictor,
        ILogger<DailyRunner> logger)
    {
        _priceRepository    = priceRepository;
        _analysisRepository = analysisRepository;
        _modelTrainer       = modelTrainer;
        _analysisEngine     = analysisEngine;
        _modelPredictor     = modelPredictor;
        _logger             = logger;
    }

    /// <summary>
    /// Runs the complete daily analysis pipeline.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("=== GoldAI Daily Runner — {Date} ===", DateTime.UtcNow.Date);

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

        // ── 2. Train / refresh model ──────────────────────────────────────────
        bool shouldRetrain = !_modelPredictor.IsModelAvailable()
            || ShouldRetrain(minRecords);

        if (shouldRetrain)
        {
            _logger.LogInformation("Training ML model on {N} records...", minRecords);
            await _modelTrainer.TrainAsync(goldPrices, silverPrices, usdPrices, ct);
            _logger.LogInformation("Model training complete.");
        }

        // ── 3. Run analysis ───────────────────────────────────────────────────
        _logger.LogInformation("Running daily analysis...");
        var result = await _analysisEngine.RunAsync(goldPrices, silverPrices, usdPrices, ct);

        // ── 4. Persist result ─────────────────────────────────────────────────
        await _analysisRepository.SaveAsync(result, ct);
        _logger.LogInformation("Analysis result saved.");

        // ── 5. Print summary ──────────────────────────────────────────────────
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
}
