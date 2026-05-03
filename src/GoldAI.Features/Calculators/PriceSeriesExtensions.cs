using GoldAI.Domain.Models;

namespace GoldAI.Features.Calculators;

/// <summary>
/// Utility helpers that operate on ordered sequences of close prices.
/// </summary>
internal static class PriceSeriesExtensions
{
    /// <summary>Returns the closing prices as a float array (oldest → newest).</summary>
    internal static float[] Closes(this IReadOnlyList<AssetPrice> prices) =>
        prices.Select(p => (float)p.Close).ToArray();

    /// <summary>Calculates daily log returns for a sequence of prices.</summary>
    internal static float[] LogReturns(this float[] closes)
    {
        if (closes.Length < 2) return Array.Empty<float>();
        var returns = new float[closes.Length - 1];
        for (int i = 0; i < returns.Length; i++)
            returns[i] = closes[i + 1] > 0 && closes[i] > 0
                ? MathF.Log(closes[i + 1] / closes[i])
                : 0f;
        return returns;
    }

    /// <summary>Simple moving average of the last <paramref name="period"/> values.</summary>
    internal static float Sma(this float[] values, int period)
    {
        int n = values.Length;
        if (n < period) period = n;
        if (period == 0) return 0f;
        float sum = 0;
        for (int i = n - period; i < n; i++) sum += values[i];
        return sum / period;
    }

    /// <summary>Population standard deviation of the last <paramref name="period"/> values.</summary>
    internal static float StdDev(this float[] values, int period)
    {
        int n = values.Length;
        if (n < period) period = n;
        if (period < 2) return 0f;
        float[] slice = values.Skip(n - period).Take(period).ToArray();
        float mean = slice.Average();
        float variance = slice.Average(v => (v - mean) * (v - mean));
        return MathF.Sqrt(variance);
    }

    /// <summary>Pearson correlation of two same-length float arrays.</summary>
    internal static float Correlation(float[] x, float[] y)
    {
        int n = Math.Min(x.Length, y.Length);
        if (n < 2) return 0f;
        float[] xs = x.TakeLast(n).ToArray();
        float[] ys = y.TakeLast(n).ToArray();
        float mx = xs.Average(), my = ys.Average();
        float num = xs.Zip(ys, (a, b) => (a - mx) * (b - my)).Sum();
        float dx = MathF.Sqrt(xs.Sum(a => (a - mx) * (a - mx)));
        float dy = MathF.Sqrt(ys.Sum(b => (b - my) * (b - my)));
        float denom = dx * dy;
        return denom < 1e-9f ? 0f : num / denom;
    }

    /// <summary>
    /// Computes a normalised linear regression slope over <paramref name="period"/> values.
    /// Divides by the mean to make it scale-independent.
    /// </summary>
    internal static float NormalisedSlope(this float[] values, int period)
    {
        int n = values.Length;
        if (n < period) period = n;
        if (period < 2) return 0f;
        float[] slice = values.Skip(n - period).ToArray();
        float xMean = (period - 1) / 2f;
        float yMean = slice.Average();
        if (yMean == 0f) return 0f;
        float num = 0f, denom = 0f;
        for (int i = 0; i < period; i++)
        {
            num += (i - xMean) * (slice[i] - yMean);
            denom += (i - xMean) * (i - xMean);
        }
        return denom < 1e-9f ? 0f : num / denom / yMean;
    }
}
