namespace GoldAI.Domain.Models;

/// <summary>
/// Contains all computed analytical features for a single asset on a given day.
/// These features are derived from raw price data across three analytical layers:
/// Layer 1 (Market Features), Layer 2 (Cross-Asset Intelligence), and Layer 3 (Seasonality).
/// </summary>
public class MarketFeatures
{
    public AssetType Asset { get; set; }
    public DateTime Date { get; set; }

    // ── Layer 1: Momentum ──────────────────────────────────────────────────────
    /// <summary>Percentage price change over 5 days.</summary>
    public float Momentum5d { get; set; }
    /// <summary>Percentage price change over 10 days.</summary>
    public float Momentum10d { get; set; }
    /// <summary>Percentage price change over 20 days.</summary>
    public float Momentum20d { get; set; }

    // ── Layer 1: Volatility ────────────────────────────────────────────────────
    /// <summary>Average True Range over 14 days (normalised by close price).</summary>
    public float Atr14d { get; set; }
    /// <summary>Standard deviation of daily returns over 20 days.</summary>
    public float ReturnStdDev20d { get; set; }

    // ── Layer 1: Mean Reversion ───────────────────────────────────────────────
    /// <summary>Percentage deviation from the 20-day simple moving average.</summary>
    public float DeviationFromSma20 { get; set; }
    /// <summary>Percentage deviation from the 50-day simple moving average.</summary>
    public float DeviationFromSma50 { get; set; }

    // ── Layer 1: Trend Strength ────────────────────────────────────────────────
    /// <summary>Slope (linear regression coefficient) of closes over 20 days, normalised.</summary>
    public float TrendSlope20d { get; set; }
    /// <summary>Ratio of SMA20 to SMA50; > 1 indicates a bullish configuration.</summary>
    public float SmaCrossRatio { get; set; }

    // ── Layer 2: Cross-Asset Intelligence ─────────────────────────────────────
    /// <summary>Rolling 20-day Pearson correlation between this asset and USD.</summary>
    public float CorrelationWithUsd20d { get; set; }
    /// <summary>Rolling 20-day Pearson correlation between Gold and Silver.</summary>
    public float CorrelationGoldSilver20d { get; set; }
    /// <summary>5-day momentum of USD (used as external signal for Gold/Silver).</summary>
    public float UsdMomentum5d { get; set; }
    /// <summary>5-day momentum of Silver (acts as leading indicator for Gold).</summary>
    public float SilverMomentum5d { get; set; }
    /// <summary>Lead-lag score: Silver 1-day return leads Gold by 1 day.</summary>
    public float SilverLeadGoldScore { get; set; }
    /// <summary>Relative strength of Gold vs Silver over 20 days.</summary>
    public float GoldSilverRatio20d { get; set; }

    // ── Layer 3: Seasonality ──────────────────────────────────────────────────
    /// <summary>Calendar month encoded as a float (1–12).</summary>
    public float MonthOfYear { get; set; }
    /// <summary>ISO week number (1–53).</summary>
    public float WeekOfYear { get; set; }
    /// <summary>
    /// Binary flag for months that historically show stronger demand
    /// in precious metals markets (January, February, November, December).
    /// </summary>
    public float IsSeasonallyStrong { get; set; }
}
