using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GoldAI.App.Services;

/// <summary>
/// Evaluates historical prediction accuracy by comparing saved ML predictions
/// against the actual prices observed on the following trading day.
///
/// If the directional accuracy (predicted UP/DOWN vs actual UP/DOWN) falls below
/// <see cref="AccuracyThreshold"/> over the most recent evaluation window, the
/// checker recommends forcing a model retrain so the ML layer can re-calibrate.
/// </summary>
public class PredictionAccuracyChecker
{
    /// <summary>
    /// Minimum directional accuracy (0–1) below which a retrain is triggered.
    /// Default: 0.50 – i.e. the model must do at least as well as a coin-flip.
    /// </summary>
    public const float AccuracyThreshold = 0.50f;

    /// <summary>Number of recent days used for the accuracy evaluation window.</summary>
    public const int EvaluationWindowDays = 14;

    private readonly IAnalysisRepository _analysisRepo;
    private readonly IPriceRepository _priceRepo;
    private readonly ILogger<PredictionAccuracyChecker> _logger;

    public PredictionAccuracyChecker(
        IAnalysisRepository analysisRepo,
        IPriceRepository priceRepo,
        ILogger<PredictionAccuracyChecker> logger)
    {
        _analysisRepo = analysisRepo;
        _priceRepo    = priceRepo;
        _logger       = logger;
    }

    /// <summary>
    /// Evaluates recent prediction accuracy and returns <c>true</c> when the model
    /// should be retrained due to poor performance.
    /// </summary>
    public async Task<bool> ShouldForceRetrainAsync(CancellationToken ct = default)
    {
        var to   = DateTime.UtcNow.Date.AddDays(-1); // yesterday
        var from = to.AddDays(-EvaluationWindowDays);

        var analyses = await _analysisRepo.GetRangeAsync(from, to, ct);
        if (analyses.Count < 5)
        {
            // Not enough history to evaluate – skip
            _logger.LogDebug(
                "PredictionAccuracyChecker: only {N} analysis records in window, skipping check.",
                analyses.Count);
            return false;
        }

        var goldPrices  = await _priceRepo.GetRangeAsync(AssetType.Gold,   from, to.AddDays(1), ct);
        var silverPrices= await _priceRepo.GetRangeAsync(AssetType.Silver, from, to.AddDays(1), ct);

        // Build date-indexed price lookups
        var goldByDate   = goldPrices.ToDictionary(p => p.Date.Date);
        var silverByDate = silverPrices.ToDictionary(p => p.Date.Date);

        int correct = 0, total = 0;

        // For each analysis on day D, check if day D's prediction matched day D+1's price
        var sortedAnalyses = analyses.OrderBy(a => a.Date).ToList();
        for (int i = 0; i < sortedAnalyses.Count - 1; i++)
        {
            var analysis  = sortedAnalyses[i];
            var nextDay   = analysis.Date.Date.AddDays(1);

            total += EvaluateDirectionAccuracy(
                analysis.GoldPrediction,
                AssetType.Gold,
                analysis.GoldPrice,
                nextDay,
                goldByDate,
                ref correct);

            total += EvaluateDirectionAccuracy(
                analysis.SilverPrediction,
                AssetType.Silver,
                analysis.SilverPrice,
                nextDay,
                silverByDate,
                ref correct);
        }

        if (total == 0)
        {
            _logger.LogDebug("PredictionAccuracyChecker: no comparable prediction pairs found.");
            return false;
        }

        float accuracy = (float)correct / total;
        _logger.LogInformation(
            "Prediction accuracy over last {Window} days: {Correct}/{Total} = {Accuracy:P1}",
            EvaluationWindowDays, correct, total, accuracy);

        if (accuracy < AccuracyThreshold)
        {
            _logger.LogWarning(
                "Prediction accuracy {Accuracy:P1} is below threshold {Threshold:P1}. " +
                "Forcing model retrain.",
                accuracy, AccuracyThreshold);
            return true;
        }

        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int EvaluateDirectionAccuracy(
        MLPrediction? prediction,
        AssetType asset,
        decimal todayClose,
        DateTime nextDay,
        Dictionary<DateTime, AssetPrice> priceByDate,
        ref int correct)
    {
        if (prediction is null || todayClose == 0)
            return 0;

        if (!priceByDate.TryGetValue(nextDay, out var nextPrice))
            return 0; // No actual data for next day – skip

        bool predictedUp = prediction.UpProbability > 0.5f;
        bool actuallyUp  = nextPrice.Close > todayClose;

        _logger.LogDebug(
            "{Asset}: predicted {Predicted}, actual {Actual} ({TodayClose:N0} → {NextClose:N0})",
            asset,
            predictedUp ? "UP" : "DOWN",
            actuallyUp  ? "UP" : "DOWN",
            todayClose,
            nextPrice.Close);

        if (predictedUp == actuallyUp)
            correct++;

        return 1;
    }
}
