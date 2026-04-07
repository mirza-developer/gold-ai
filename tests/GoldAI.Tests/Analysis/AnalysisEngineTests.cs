using GoldAI.Analysis;
using GoldAI.Domain.Models;
using GoldAI.Features;
using GoldAI.ML;
using Xunit;

namespace GoldAI.Tests.Analysis;

/// <summary>
/// Integration-style tests for the full AnalysisEngine pipeline.
/// These tests do NOT require a database or trained ML model.
/// When no model is available the predictor returns neutral 0.5 probabilities.
/// </summary>
public class AnalysisEngineTests
{
    private static IReadOnlyList<AssetPrice> MakePrices(
        AssetType asset, int count, double start, double step)
    {
        var date = new DateTime(2024, 1, 1);
        return Enumerable.Range(0, count).Select(i => new AssetPrice
        {
            Asset  = asset,
            Date   = date.AddDays(i),
            Open   = (decimal)(start + i * step),
            High   = (decimal)(start + i * step + 5),
            Low    = (decimal)(start + i * step - 5),
            Close  = (decimal)(start + i * step),
            Volume = 1000m
        }).ToList();
    }

    [Fact]
    public async Task RunAsync_Returns_Result_With_All_Fields_Populated()
    {
        var gold   = MakePrices(AssetType.Gold,   60, 1800, 2);
        var silver = MakePrices(AssetType.Silver, 60, 22,   0.1);
        var usd    = MakePrices(AssetType.USD,    60, 1.05, 0.001);

        var engine = new AnalysisEngine(
            new FeatureCalculator(),
            new ModelPredictor(modelDirectory: "/tmp/nonexistent_models"));

        var result = await engine.RunAsync(gold, silver, usd);

        Assert.NotNull(result);
        Assert.Equal(DateTime.UtcNow.Date, result.Date);

        // Prices are set correctly
        Assert.Equal(gold[^1].Close,   result.GoldPrice);
        Assert.Equal(silver[^1].Close, result.SilverPrice);
        Assert.Equal(usd[^1].Close,    result.UsdPrice);

        // Features are computed for all three assets
        Assert.NotNull(result.GoldFeatures);
        Assert.NotNull(result.SilverFeatures);
        Assert.NotNull(result.UsdFeatures);

        // ML predictions are produced (neutral 0.5 when no model available)
        Assert.NotNull(result.GoldPrediction);
        Assert.NotNull(result.SilverPrediction);
        Assert.NotNull(result.UsdPrediction);

        // Portfolio allocation is produced
        Assert.NotNull(result.Allocation);

        // Weights sum to 1
        float total = result.Allocation!.GoldWeight
                    + result.Allocation.SilverWeight
                    + result.Allocation.UsdWeight;
        Assert.Equal(1f, total, precision: 4);

        // Risk score is in [0, 1]
        Assert.InRange(result.OverallRiskScore, 0f, 1f);
    }

    [Fact]
    public async Task RunAsync_Produces_Json_Output()
    {
        var gold   = MakePrices(AssetType.Gold,   60, 1800, 2);
        var silver = MakePrices(AssetType.Silver, 60, 22,   0.1);
        var usd    = MakePrices(AssetType.USD,    60, 1.05, 0.001);

        var engine = new AnalysisEngine(
            new FeatureCalculator(),
            new ModelPredictor(modelDirectory: "/tmp/nonexistent_models"));

        var result = await engine.RunAsync(gold, silver, usd);
        string json = result.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("GoldPrice", json);
        Assert.Contains("Allocation", json);
    }
}
