using GoldAI.Domain.Models;

namespace GoldAI.Analysis.Engines;

/// <summary>
/// Computes a hybrid asset score that combines quantitative indicators
/// with ML-predicted probabilities.
///
/// Formula (conceptually):
///   Score = TrendStrength + UpProbability + SeasonalitySignal
///           − CrashProbability − VolatilityRisk
/// </summary>
public class ScoringEngine
{
    // Weights for the hybrid scoring formula
    private const float TrendWeight       = 0.20f;
    private const float UpProbWeight      = 0.30f;
    private const float SeasonalityWeight = 0.10f;
    private const float CrashPenalty      = 0.25f;
    private const float VolatilityPenalty = 0.15f;

    /// <summary>
    /// Calculates a composite score for one asset.
    /// Returns a value roughly in [−1, 1]; higher = more attractive.
    /// </summary>
    public float Score(MarketFeatures features, MLPrediction prediction)
    {
        float trendStrength  = Clamp(features.TrendSlope20d * 5f);   // scale slope to ≈ [-1, 1]
        float seasonality    = features.IsSeasonallyStrong * 0.5f;   // 0 or 0.5
        float upProb         = prediction.UpProbability * 2f - 1f;   // remap [0,1] → [-1, 1]
        float crashRisk      = prediction.CrashProbability;
        float volatilityRisk = Clamp(features.ReturnStdDev20d * 20f);

        float score =
              TrendWeight       * trendStrength
            + UpProbWeight      * upProb
            + SeasonalityWeight * seasonality
            - CrashPenalty      * crashRisk
            - VolatilityPenalty * volatilityRisk;

        return Clamp(score);
    }

    private static float Clamp(float v) => Math.Clamp(v, -1f, 1f);
}
