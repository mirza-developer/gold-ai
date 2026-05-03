namespace GoldAI.Domain.Models;

/// <summary>
/// Probabilistic forecasts produced by the ML.NET model for a single asset.
/// </summary>
public class MLPrediction
{
    public AssetType Asset { get; set; }
    public DateTime Date { get; set; }

    /// <summary>Probability the asset price will increase in the short term.</summary>
    public float UpProbability { get; set; }

    /// <summary>Probability of a significant drop (roughly &gt; 5 %).</summary>
    public float CrashProbability { get; set; }

    /// <summary>Probability that the current trend will continue.</summary>
    public float TrendContinuationProbability { get; set; }

    /// <summary>Probability that price will revert toward its historical average.</summary>
    public float MeanReversionProbability { get; set; }

    /// <summary>Probability that market volatility will expand sharply.</summary>
    public float VolatilityExpansionProbability { get; set; }
}
