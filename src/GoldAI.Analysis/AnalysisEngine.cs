using GoldAI.Analysis.Engines;
using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;

namespace GoldAI.Analysis;

/// <summary>
/// Orchestrates the full daily analysis pipeline:
/// 1. Feature extraction (Layer 1, 2, 3)
/// 2. ML inference
/// 3. Hybrid scoring (Quant + AI)
/// 4. Portfolio allocation
/// 5. Risk management
/// </summary>
public class AnalysisEngine : IAnalysisEngine
{
    private readonly IFeatureCalculator _featureCalculator;
    private readonly IModelPredictor    _modelPredictor;
    private readonly ScoringEngine      _scoringEngine;
    private readonly PortfolioOptimizer _portfolioOptimizer;
    private readonly RiskManager        _riskManager;

    public AnalysisEngine(
        IFeatureCalculator featureCalculator,
        IModelPredictor    modelPredictor)
    {
        _featureCalculator  = featureCalculator;
        _modelPredictor     = modelPredictor;
        _scoringEngine      = new ScoringEngine();
        _portfolioOptimizer = new PortfolioOptimizer();
        _riskManager        = new RiskManager();
    }

    /// <inheritdoc />
    public Task<DailyAnalysisResult> RunAsync(
        IReadOnlyList<AssetPrice> goldPrices,
        IReadOnlyList<AssetPrice> silverPrices,
        IReadOnlyList<AssetPrice> usdPrices,
        CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        // ── Step 1: Feature extraction ────────────────────────────────────────
        var goldFeatures   = _featureCalculator.Calculate(goldPrices,   goldPrices, silverPrices, usdPrices);
        var silverFeatures = _featureCalculator.Calculate(silverPrices, goldPrices, silverPrices, usdPrices);
        var usdFeatures    = _featureCalculator.Calculate(usdPrices,    goldPrices, silverPrices, usdPrices);

        // ── Step 2: ML inference ──────────────────────────────────────────────
        var goldPrediction   = _modelPredictor.Predict(goldFeatures);
        var silverPrediction = _modelPredictor.Predict(silverFeatures);
        var usdPrediction    = _modelPredictor.Predict(usdFeatures);

        // ── Step 3: Hybrid scoring ────────────────────────────────────────────
        float goldScore   = _scoringEngine.Score(goldFeatures,   goldPrediction);
        float silverScore = _scoringEngine.Score(silverFeatures, silverPrediction);
        float usdScore    = _scoringEngine.Score(usdFeatures,    usdPrediction);

        // ── Step 4: Preliminary allocation ───────────────────────────────────
        var preliminary = _portfolioOptimizer.Allocate(today, goldScore, silverScore, usdScore);

        // ── Step 5: Risk adjustment ───────────────────────────────────────────
        var (allocation, riskScore, crashWarning, highVolWarning) =
            _riskManager.AdjustForRisk(
                preliminary,
                goldPrediction, silverPrediction,
                goldFeatures,   silverFeatures);

        // ── Assemble result ───────────────────────────────────────────────────
        var result = new DailyAnalysisResult
        {
            Date      = today,
            GoldPrice   = goldPrices.Count   > 0 ? goldPrices[^1].Close   : 0,
            SilverPrice = silverPrices.Count > 0 ? silverPrices[^1].Close : 0,
            UsdPrice    = usdPrices.Count    > 0 ? usdPrices[^1].Close    : 0,

            GoldFeatures   = goldFeatures,
            SilverFeatures = silverFeatures,
            UsdFeatures    = usdFeatures,

            GoldPrediction   = goldPrediction,
            SilverPrediction = silverPrediction,
            UsdPrediction    = usdPrediction,

            GoldScore   = goldScore,
            SilverScore = silverScore,
            UsdScore    = usdScore,

            Allocation           = allocation,
            OverallRiskScore     = riskScore,
            CrashWarning         = crashWarning,
            HighVolatilityWarning = highVolWarning,
        };

        return Task.FromResult(result);
    }
}
