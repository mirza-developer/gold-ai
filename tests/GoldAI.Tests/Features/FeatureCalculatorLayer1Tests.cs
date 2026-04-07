using GoldAI.Domain.Models;
using GoldAI.Features;
using Xunit;

namespace GoldAI.Tests.Features;

/// <summary>
/// Tests for Layer 1 – Market Feature Extraction.
/// </summary>
public class FeatureCalculatorLayer1Tests
{
    private static IReadOnlyList<AssetPrice> MakePrices(
        AssetType asset, double[] closes, DateTime? startDate = null)
    {
        var date = startDate ?? new DateTime(2024, 1, 1);
        return closes.Select((c, i) => new AssetPrice
        {
            Asset  = asset,
            Date   = date.AddDays(i),
            Open   = (decimal)c,
            High   = (decimal)(c * 1.01),
            Low    = (decimal)(c * 0.99),
            Close  = (decimal)c,
            Volume = 1000m
        }).ToList();
    }

    private static double[] GenerateTrend(int n, double start, double step = 1.0) =>
        Enumerable.Range(0, n).Select(i => start + i * step).ToArray();

    private readonly FeatureCalculator _calculator = new();

    [Fact]
    public void Calculate_Returns_Features_For_Latest_Date()
    {
        var closes = GenerateTrend(60, 1800, 2);
        var prices = MakePrices(AssetType.Gold, closes);

        var features = _calculator.Calculate(prices, prices,
            MakePrices(AssetType.Silver, closes.Select(c => c / 80).ToArray()),
            MakePrices(AssetType.USD, closes.Select(_ => 1.05).ToArray()));

        Assert.Equal(AssetType.Gold, features.Asset);
        Assert.Equal(prices[^1].Date, features.Date);
    }

    [Fact]
    public void Momentum5d_Is_Positive_For_Rising_Prices()
    {
        var closes = GenerateTrend(60, 1800, 5);  // steadily rising
        var prices = MakePrices(AssetType.Gold, closes);

        var features = _calculator.Calculate(prices, prices,
            MakePrices(AssetType.Silver, closes.Select(c => c / 80).ToArray()),
            MakePrices(AssetType.USD, closes.Select(_ => 1.05).ToArray()));

        Assert.True(features.Momentum5d > 0,
            $"Expected positive 5-day momentum for rising prices but got {features.Momentum5d}");
    }

    [Fact]
    public void Momentum5d_Is_Negative_For_Falling_Prices()
    {
        var closes = GenerateTrend(60, 2000, -5); // steadily falling
        var prices = MakePrices(AssetType.Gold, closes);

        var features = _calculator.Calculate(prices, prices,
            MakePrices(AssetType.Silver, closes.Select(c => c / 80).ToArray()),
            MakePrices(AssetType.USD, closes.Select(_ => 1.05).ToArray()));

        Assert.True(features.Momentum5d < 0,
            $"Expected negative 5-day momentum for falling prices but got {features.Momentum5d}");
    }

    [Fact]
    public void DeviationFromSma20_Is_Positive_When_Price_Above_Average()
    {
        // Flat then spike up
        double[] closes = Enumerable.Range(0, 55).Select(i => i < 50 ? 1800.0 : 2000.0).ToArray();
        var prices = MakePrices(AssetType.Gold, closes);

        var features = _calculator.Calculate(prices, prices,
            MakePrices(AssetType.Silver, closes.Select(c => c / 80).ToArray()),
            MakePrices(AssetType.USD, closes.Select(_ => 1.05).ToArray()));

        Assert.True(features.DeviationFromSma20 > 0,
            $"Expected positive SMA20 deviation but got {features.DeviationFromSma20}");
    }

    [Fact]
    public void Atr14d_Is_Greater_For_Volatile_Prices()
    {
        var stableCloses = GenerateTrend(60, 1800, 1);
        var volatileCloses = Enumerable.Range(0, 60)
            .Select(i => 1800.0 + (i % 2 == 0 ? 50 : -50))
            .ToArray();

        var stablePrices   = MakePrices(AssetType.Gold, stableCloses);
        var volatilePrices = MakePrices(AssetType.Gold, volatileCloses);

        var stableRef  = MakePrices(AssetType.Silver, stableCloses.Select(c => c / 80).ToArray());
        var usdRef     = MakePrices(AssetType.USD, stableCloses.Select(_ => 1.05).ToArray());

        var stableFeatures   = _calculator.Calculate(stablePrices,   stablePrices,   stableRef, usdRef);
        var volatileFeatures = _calculator.Calculate(volatilePrices, volatilePrices, stableRef, usdRef);

        Assert.True(volatileFeatures.Atr14d > stableFeatures.Atr14d,
            $"Volatile ATR {volatileFeatures.Atr14d} should be > stable ATR {stableFeatures.Atr14d}");
    }

    [Fact]
    public void TrendSlope20d_Is_Positive_For_Rising_Series()
    {
        var closes = GenerateTrend(60, 1800, 3);
        var prices = MakePrices(AssetType.Gold, closes);

        var features = _calculator.Calculate(prices, prices,
            MakePrices(AssetType.Silver, closes.Select(c => c / 80).ToArray()),
            MakePrices(AssetType.USD, closes.Select(_ => 1.05).ToArray()));

        Assert.True(features.TrendSlope20d > 0,
            $"Expected positive trend slope but got {features.TrendSlope20d}");
    }

    [Fact]
    public void Calculate_Throws_When_Prices_Empty()
    {
        var empty = Array.Empty<AssetPrice>();
        var dummy = MakePrices(AssetType.Gold, [1800, 1800]);

        Assert.Throws<ArgumentException>(() =>
            _calculator.Calculate(empty, dummy, dummy, dummy));
    }
}
