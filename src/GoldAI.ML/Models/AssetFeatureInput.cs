using Microsoft.ML.Data;

namespace GoldAI.ML.Models;

/// <summary>
/// Input schema for the ML.NET pipeline.
/// Each property corresponds to one feature in <see cref="GoldAI.Domain.Models.MarketFeatures"/>.
/// </summary>
public class AssetFeatureInput
{
    // Layer 1 – Momentum
    [LoadColumn(0)]  public float Momentum5d { get; set; }
    [LoadColumn(1)]  public float Momentum10d { get; set; }
    [LoadColumn(2)]  public float Momentum20d { get; set; }

    // Layer 1 – Volatility
    [LoadColumn(3)]  public float Atr14d { get; set; }
    [LoadColumn(4)]  public float ReturnStdDev20d { get; set; }

    // Layer 1 – Mean Reversion
    [LoadColumn(5)]  public float DeviationFromSma20 { get; set; }
    [LoadColumn(6)]  public float DeviationFromSma50 { get; set; }

    // Layer 1 – Trend Strength
    [LoadColumn(7)]  public float TrendSlope20d { get; set; }
    [LoadColumn(8)]  public float SmaCrossRatio { get; set; }

    // Layer 2 – Cross-Asset
    [LoadColumn(9)]  public float CorrelationWithUsd20d { get; set; }
    [LoadColumn(10)] public float CorrelationGoldSilver20d { get; set; }
    [LoadColumn(11)] public float UsdMomentum5d { get; set; }
    [LoadColumn(12)] public float SilverMomentum5d { get; set; }
    [LoadColumn(13)] public float SilverLeadGoldScore { get; set; }
    [LoadColumn(14)] public float GoldSilverRatio20d { get; set; }

    // Layer 3 – Seasonality
    [LoadColumn(15)] public float MonthOfYear { get; set; }
    [LoadColumn(16)] public float WeekOfYear { get; set; }
    [LoadColumn(17)] public float IsSeasonallyStrong { get; set; }

    // Labels (used during training; ignored during inference)
    [LoadColumn(18)] public bool LabelUp { get; set; }
    [LoadColumn(19)] public bool LabelCrash { get; set; }
    [LoadColumn(20)] public bool LabelTrendContinuation { get; set; }
    [LoadColumn(21)] public bool LabelMeanReversion { get; set; }
    [LoadColumn(22)] public bool LabelVolatilityExpansion { get; set; }
}
