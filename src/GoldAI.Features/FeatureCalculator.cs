using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using GoldAI.Features.Calculators;

namespace GoldAI.Features;

/// <summary>
/// Implements feature engineering across all three analytical layers:
/// Layer 1 (Market Features), Layer 2 (Cross-Asset Intelligence), Layer 3 (Seasonality).
/// </summary>
public class FeatureCalculator : IFeatureCalculator
{
    // Minimum number of price records required to produce a meaningful feature set.
    private const int MinHistory = 50;

    /// <inheritdoc />
    public MarketFeatures Calculate(
        IReadOnlyList<AssetPrice> prices,
        IReadOnlyList<AssetPrice> goldPrices,
        IReadOnlyList<AssetPrice> silverPrices,
        IReadOnlyList<AssetPrice> usdPrices)
    {
        if (prices.Count == 0)
            throw new ArgumentException("Price list must not be empty.", nameof(prices));

        var latest = prices[^1];
        var closes = prices.Closes();
        var returns = closes.LogReturns();

        var features = new MarketFeatures
        {
            Asset = latest.Asset,
            Date = latest.Date
        };

        // ── Layer 1: Momentum ─────────────────────────────────────────────────
        features.Momentum5d  = Momentum(closes, 5);
        features.Momentum10d = Momentum(closes, 10);
        features.Momentum20d = Momentum(closes, 20);

        // ── Layer 1: Volatility ───────────────────────────────────────────────
        features.Atr14d         = AverageTrueRange(prices, 14);
        features.ReturnStdDev20d = returns.StdDev(20);

        // ── Layer 1: Mean Reversion ───────────────────────────────────────────
        float sma20 = closes.Sma(20);
        float sma50 = closes.Sma(50);
        float lastClose = closes[^1];
        features.DeviationFromSma20 = sma20 > 0 ? (lastClose - sma20) / sma20 : 0f;
        features.DeviationFromSma50 = sma50 > 0 ? (lastClose - sma50) / sma50 : 0f;

        // ── Layer 1: Trend Strength ───────────────────────────────────────────
        features.TrendSlope20d = closes.NormalisedSlope(20);
        features.SmaCrossRatio = sma50 > 0 ? sma20 / sma50 : 1f;

        // ── Layer 2: Cross-Asset Intelligence ────────────────────────────────
        var goldCloses   = goldPrices.Closes();
        var silverCloses = silverPrices.Closes();
        var usdCloses    = usdPrices.Closes();

        var goldReturns   = goldCloses.LogReturns();
        var silverReturns = silverCloses.LogReturns();
        var usdReturns    = usdCloses.LogReturns();

        // Correlations (20-day rolling window on returns)
        features.CorrelationWithUsd20d      = Correlation20d(closes.LogReturns(), usdReturns);
        features.CorrelationGoldSilver20d   = Correlation20d(goldReturns, silverReturns);

        // USD and Silver momentum as external signals
        features.UsdMomentum5d    = Momentum(usdCloses, 5);
        features.SilverMomentum5d = Momentum(silverCloses, 5);

        // Lead-lag: Silver 1-day return yesterday → Gold return today
        features.SilverLeadGoldScore = LeadLagScore(silverReturns, goldReturns, lag: 1, window: 20);

        // Gold/Silver ratio over 20 days
        if (silverCloses.Length > 0 && goldCloses.Length > 0)
        {
            float avgSilver = silverCloses.Sma(20);
            float avgGold   = goldCloses.Sma(20);
            features.GoldSilverRatio20d = avgSilver > 0 ? avgGold / avgSilver : 0f;
        }

        // ── Layer 3: Seasonality ──────────────────────────────────────────────
        features.MonthOfYear = latest.Date.Month;
        features.WeekOfYear  = (float)System.Globalization.ISOWeek.GetWeekOfYear(latest.Date);
        features.IsSeasonallyStrong = IsSeasonallyStrongMonth(latest.Date.Month) ? 1f : 0f;

        return features;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Percentage return from <c>period</c> days ago to today.</summary>
    private static float Momentum(float[] closes, int period)
    {
        int n = closes.Length;
        if (n <= period) return 0f;
        float past = closes[n - 1 - period];
        return past > 0 ? (closes[n - 1] - past) / past : 0f;
    }

    /// <summary>Normalised 14-day Average True Range.</summary>
    private static float AverageTrueRange(IReadOnlyList<AssetPrice> prices, int period)
    {
        int n = prices.Count;
        if (n < 2) return 0f;
        int start = Math.Max(0, n - period - 1);
        var trueRanges = new List<float>(period);
        for (int i = start + 1; i < n; i++)
        {
            float high  = (float)prices[i].High;
            float low   = (float)prices[i].Low;
            float prev  = (float)prices[i - 1].Close;
            float tr    = MathF.Max(high - low,
                          MathF.Max(MathF.Abs(high - prev),
                                    MathF.Abs(low  - prev)));
            trueRanges.Add(tr);
        }
        if (trueRanges.Count == 0) return 0f;
        float atr = trueRanges.Average();
        float close = (float)prices[n - 1].Close;
        return close > 0 ? atr / close : 0f;
    }

    /// <summary>Pearson correlation on the last 20 daily returns.</summary>
    private static float Correlation20d(float[] returnsA, float[] returnsB)
    {
        int n = Math.Min(returnsA.Length, returnsB.Length);
        if (n < 5) return 0f;
        int window = Math.Min(n, 20);
        return PriceSeriesExtensions.Correlation(
            returnsA.TakeLast(window).ToArray(),
            returnsB.TakeLast(window).ToArray());
    }

    /// <summary>
    /// Measures how predictive <paramref name="leadReturns"/> lagged by <paramref name="lag"/>
    /// days are for <paramref name="lagReturns"/> over the most recent <paramref name="window"/> days.
    /// Returns the Pearson correlation of the shifted pairs.
    /// </summary>
    private static float LeadLagScore(
        float[] leadReturns, float[] lagReturns, int lag, int window)
    {
        int n = Math.Min(leadReturns.Length, lagReturns.Length);
        if (n < lag + 2) return 0f;
        int actualWindow = Math.Min(n - lag, window);
        if (actualWindow < 2) return 0f;
        float[] x = leadReturns.Skip(n - actualWindow - lag).Take(actualWindow).ToArray();
        float[] y = lagReturns.Skip(n - actualWindow).Take(actualWindow).ToArray();
        return PriceSeriesExtensions.Correlation(x, y);
    }

    /// <summary>
    /// Returns true for months historically associated with stronger precious-metals demand
    /// (January, February, November, December).
    /// </summary>
    private static bool IsSeasonallyStrongMonth(int month) =>
        month is 1 or 2 or 11 or 12;
}
