using GoldAI.Domain.Models;

namespace GoldAI.Analysis.Engines;

/// <summary>
/// Converts asset scores into a portfolio weight allocation.
/// Uses a softmax-inspired approach: higher scores → higher weights.
/// </summary>
public class PortfolioOptimizer
{
    private const float MinWeight   = 0.05f;  // floor for each asset
    private const float Temperature = 2.0f;   // softmax temperature (higher = smoother)

    // Scores from ScoringEngine are in [-1, 1]. Add this offset to shift them into the
    // strictly-positive range required by the softmax allocation step.
    private const float ScoreOffset = 1f;

    /// <summary>
    /// Converts per-asset scores into a normalised portfolio allocation.
    /// </summary>
    public PortfolioAllocation Allocate(
        DateTime date,
        float goldScore,
        float silverScore,
        float usdScore)
    {
        // Shift scores to positive range then apply softmax
        float[] scores = [goldScore + ScoreOffset, silverScore + ScoreOffset, usdScore + ScoreOffset];
        float[] weights = Softmax(scores, Temperature);

        // Enforce minimum weight floors and renormalise
        for (int i = 0; i < weights.Length; i++)
            weights[i] = Math.Max(weights[i], MinWeight);

        float total = weights.Sum();
        for (int i = 0; i < weights.Length; i++)
            weights[i] /= total;

        return new PortfolioAllocation
        {
            Date         = date,
            GoldWeight   = weights[0],
            SilverWeight = weights[1],
            UsdWeight    = weights[2],
        };
    }

    private static float[] Softmax(float[] values, float temperature)
    {
        float max = values.Max();
        float[] exps = values.Select(v => MathF.Exp((v - max) / temperature)).ToArray();
        float sum = exps.Sum();
        return sum < 1e-9f
            ? values.Select(_ => 1f / values.Length).ToArray()
            : exps.Select(e => e / sum).ToArray();
    }
}
