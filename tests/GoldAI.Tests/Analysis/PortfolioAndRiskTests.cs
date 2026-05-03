using GoldAI.Analysis.Engines;
using GoldAI.Domain.Models;
using Xunit;

namespace GoldAI.Tests.Analysis;

/// <summary>
/// Tests for the portfolio optimizer and risk manager.
/// </summary>
public class PortfolioAndRiskTests
{
    private readonly PortfolioOptimizer _optimizer = new();
    private readonly RiskManager        _riskManager = new();

    // ── Portfolio Optimizer ───────────────────────────────────────────────────

    [Fact]
    public void Allocate_Weights_Sum_To_One()
    {
        var allocation = _optimizer.Allocate(DateTime.Today, 0.5f, 0.3f, 0.2f);
        float total = allocation.GoldWeight + allocation.SilverWeight + allocation.UsdWeight;
        Assert.Equal(1f, total, precision: 4);
    }

    [Fact]
    public void Allocate_Higher_Score_Gets_Higher_Weight()
    {
        // Gold has the highest score
        var allocation = _optimizer.Allocate(DateTime.Today, 0.9f, 0.1f, 0.0f);
        Assert.True(allocation.GoldWeight > allocation.SilverWeight,
            "Gold with higher score should receive more weight than Silver.");
        Assert.True(allocation.GoldWeight > allocation.UsdWeight,
            "Gold with higher score should receive more weight than USD.");
    }

    [Fact]
    public void Allocate_Each_Weight_Is_Positive()
    {
        var allocation = _optimizer.Allocate(DateTime.Today, -0.5f, -0.8f, 0.9f);
        Assert.True(allocation.GoldWeight   >= 0f);
        Assert.True(allocation.SilverWeight >= 0f);
        Assert.True(allocation.UsdWeight    >= 0f);
    }

    // ── Risk Manager ─────────────────────────────────────────────────────────

    private static MLPrediction Prediction(AssetType asset, float crash) =>
        new()
        {
            Asset            = asset,
            Date             = DateTime.Today,
            CrashProbability = crash,
            UpProbability    = 0.5f,
        };

    private static MarketFeatures Features(AssetType asset, float stdDev) =>
        new()
        {
            Asset           = asset,
            Date            = DateTime.Today,
            ReturnStdDev20d = stdDev,
        };

    private static PortfolioAllocation EqualAllocation() =>
        new()
        {
            Date         = DateTime.Today,
            GoldWeight   = 0.45f,
            SilverWeight = 0.35f,
            UsdWeight    = 0.20f,
        };

    [Fact]
    public void AdjustForRisk_Weights_Still_Sum_To_One()
    {
        var (allocation, _, _, _) = _riskManager.AdjustForRisk(
            EqualAllocation(),
            Prediction(AssetType.Gold,   0.1f),
            Prediction(AssetType.Silver, 0.1f),
            Features(AssetType.Gold,   0.01f),
            Features(AssetType.Silver, 0.01f));

        float total = allocation.GoldWeight + allocation.SilverWeight + allocation.UsdWeight;
        Assert.Equal(1f, total, precision: 4);
    }

    [Fact]
    public void AdjustForRisk_Raises_CrashWarning_When_CrashProbHigh()
    {
        var (_, _, crashWarning, _) = _riskManager.AdjustForRisk(
            EqualAllocation(),
            Prediction(AssetType.Gold,   0.80f),  // very high crash probability
            Prediction(AssetType.Silver, 0.10f),
            Features(AssetType.Gold,   0.01f),
            Features(AssetType.Silver, 0.01f));

        Assert.True(crashWarning, "A crash probability of 0.80 should raise a crash warning.");
    }

    [Fact]
    public void AdjustForRisk_Increases_UsdWeight_Under_High_CrashRisk()
    {
        var initial = EqualAllocation();

        var (allocation, _, _, _) = _riskManager.AdjustForRisk(
            initial,
            Prediction(AssetType.Gold,   0.90f), // extreme crash probability for Gold
            Prediction(AssetType.Silver, 0.10f),
            Features(AssetType.Gold,   0.01f),
            Features(AssetType.Silver, 0.01f));

        Assert.True(allocation.UsdWeight > initial.UsdWeight,
            $"USD weight should increase under Gold crash risk. " +
            $"Initial: {initial.UsdWeight:F4}, Adjusted: {allocation.UsdWeight:F4}");
    }

    [Fact]
    public void AdjustForRisk_Sets_HighVolWarning_When_Volatility_High()
    {
        var (_, _, _, highVol) = _riskManager.AdjustForRisk(
            EqualAllocation(),
            Prediction(AssetType.Gold,   0.10f),
            Prediction(AssetType.Silver, 0.10f),
            Features(AssetType.Gold,   0.05f),    // 5 % daily stddev – well above threshold
            Features(AssetType.Silver, 0.05f));

        Assert.True(highVol, "High daily volatility should set the high-vol warning flag.");
    }

    [Fact]
    public void AdjustForRisk_NoWarning_Under_Normal_Conditions()
    {
        var (_, riskScore, crashWarning, highVol) = _riskManager.AdjustForRisk(
            EqualAllocation(),
            Prediction(AssetType.Gold,   0.05f),
            Prediction(AssetType.Silver, 0.05f),
            Features(AssetType.Gold,   0.008f),
            Features(AssetType.Silver, 0.008f));

        Assert.False(crashWarning, "No crash warning expected under normal conditions.");
        Assert.False(highVol,      "No high-vol warning expected under normal conditions.");
        Assert.True(riskScore < 0.5f, $"Risk score should be low under normal conditions: {riskScore}");
    }
}
