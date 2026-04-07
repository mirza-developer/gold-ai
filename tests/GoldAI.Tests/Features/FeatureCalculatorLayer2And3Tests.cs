using GoldAI.Domain.Models;
using GoldAI.Features;
using Xunit;

namespace GoldAI.Tests.Features;

/// <summary>
/// Tests for Layer 2 – Cross-Asset Intelligence and Layer 3 – Seasonality.
/// </summary>
public class FeatureCalculatorLayer2And3Tests
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

    private static double[] Trend(int n, double start, double step) =>
        Enumerable.Range(0, n).Select(i => start + i * step).ToArray();

    private readonly FeatureCalculator _calculator = new();

    [Fact]
    public void UsdMomentum5d_Reflects_Usd_Movement()
    {
        var goldCloses   = Trend(60, 1800, 1);
        var silverCloses = Trend(60, 22, 0.1);
        var usdCloses    = Trend(60, 1.05, 0.005); // rising USD

        var gold   = MakePrices(AssetType.Gold,   goldCloses);
        var silver = MakePrices(AssetType.Silver,  silverCloses);
        var usd    = MakePrices(AssetType.USD,     usdCloses);

        var features = _calculator.Calculate(gold, gold, silver, usd);

        Assert.True(features.UsdMomentum5d > 0,
            $"Rising USD should show positive UsdMomentum5d, got {features.UsdMomentum5d}");
    }

    [Fact]
    public void SilverMomentum5d_Reflects_Silver_Movement()
    {
        var goldCloses   = Trend(60, 1800, 0);
        var silverCloses = Trend(60, 22, 0.5); // rising silver
        var usdCloses    = Trend(60, 1.05, 0);

        var gold   = MakePrices(AssetType.Gold,   goldCloses);
        var silver = MakePrices(AssetType.Silver,  silverCloses);
        var usd    = MakePrices(AssetType.USD,     usdCloses);

        var features = _calculator.Calculate(gold, gold, silver, usd);

        Assert.True(features.SilverMomentum5d > 0,
            $"Rising silver should show positive SilverMomentum5d, got {features.SilverMomentum5d}");
    }

    [Fact]
    public void CorrelationGoldSilver_Is_High_For_Identical_Returns()
    {
        // Gold and Silver move identically → correlation ≈ 1
        var closes = Trend(60, 1800, 3);
        var gold   = MakePrices(AssetType.Gold,   closes);
        var silver = MakePrices(AssetType.Silver,  closes.Select(c => c / 80).ToArray());
        var usd    = MakePrices(AssetType.USD,     closes.Select(_ => 1.05).ToArray());

        var features = _calculator.Calculate(gold, gold, silver, usd);

        Assert.True(features.CorrelationGoldSilver20d > 0.8f,
            $"Identical movements should produce high Gold-Silver correlation, got {features.CorrelationGoldSilver20d}");
    }

    [Fact]
    public void IsSeasonallyStrong_Is_1_In_January()
    {
        // Start from December 3 so that the 60th (last) price falls in January.
        // 2023-12-03 + 59 days = 2024-01-31 (January).
        var closes = Trend(60, 1800, 1);
        var start  = new DateTime(2023, 12, 3);
        var gold   = MakePrices(AssetType.Gold,   closes, start);
        var silver = MakePrices(AssetType.Silver,  closes.Select(c => c / 80).ToArray(), start);
        var usd    = MakePrices(AssetType.USD,     closes.Select(_ => 1.05).ToArray(), start);

        var features = _calculator.Calculate(gold, gold, silver, usd);

        Assert.Equal(1f, features.IsSeasonallyStrong);
        Assert.Equal(1f, features.MonthOfYear); // January is month 1
    }

    [Fact]
    public void IsSeasonallyStrong_Is_0_In_June()
    {
        var closes = Trend(60, 1800, 1);
        var start  = new DateTime(2024, 6, 1); // June
        var gold   = MakePrices(AssetType.Gold,   closes, start);
        var silver = MakePrices(AssetType.Silver,  closes.Select(c => c / 80).ToArray(), start);
        var usd    = MakePrices(AssetType.USD,     closes.Select(_ => 1.05).ToArray(), start);

        var features = _calculator.Calculate(gold, gold, silver, usd);

        Assert.Equal(0f, features.IsSeasonallyStrong);
    }

    [Fact]
    public void MonthOfYear_And_WeekOfYear_Are_Correct()
    {
        var closes = Trend(60, 1800, 1);
        var start  = new DateTime(2024, 3, 15); // Mid-March
        var gold   = MakePrices(AssetType.Gold,   closes, start);
        var silver = MakePrices(AssetType.Silver,  closes.Select(c => c / 80).ToArray(), start);
        var usd    = MakePrices(AssetType.USD,     closes.Select(_ => 1.05).ToArray(), start);

        var features = _calculator.Calculate(gold, gold, silver, usd);

        // The 60th day after 2024-03-15 = 2024-05-13
        var expectedDate = start.AddDays(59);
        Assert.Equal((float)expectedDate.Month, features.MonthOfYear);

        int expectedWeek = System.Globalization.ISOWeek.GetWeekOfYear(expectedDate);
        Assert.Equal((float)expectedWeek, features.WeekOfYear);
    }
}
