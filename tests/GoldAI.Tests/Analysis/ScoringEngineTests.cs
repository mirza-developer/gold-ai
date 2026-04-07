using GoldAI.Analysis.Engines;
using GoldAI.Domain.Models;
using Xunit;

namespace GoldAI.Tests.Analysis;

/// <summary>
/// Tests for the hybrid scoring engine.
/// </summary>
public class ScoringEngineTests
{
    private readonly ScoringEngine _engine = new();

    private static MLPrediction NeutralPrediction(AssetType asset = AssetType.Gold) =>
        new()
        {
            Asset                          = asset,
            Date                           = DateTime.Today,
            UpProbability                  = 0.5f,
            CrashProbability               = 0.1f,
            TrendContinuationProbability   = 0.5f,
            MeanReversionProbability       = 0.3f,
            VolatilityExpansionProbability = 0.2f,
        };

    private static MarketFeatures NeutralFeatures(AssetType asset = AssetType.Gold) =>
        new()
        {
            Asset              = asset,
            Date               = DateTime.Today,
            TrendSlope20d      = 0f,
            ReturnStdDev20d    = 0.01f,
            IsSeasonallyStrong = 0f,
            Momentum5d         = 0f,
        };

    [Fact]
    public void Score_Is_In_Minus1_To_1_Range()
    {
        var score = _engine.Score(NeutralFeatures(), NeutralPrediction());
        Assert.InRange(score, -1f, 1f);
    }

    [Fact]
    public void Score_Increases_With_Higher_UpProbability()
    {
        var features = NeutralFeatures();

        var lowUp  = NeutralPrediction(); lowUp.UpProbability  = 0.2f;
        var highUp = NeutralPrediction(); highUp.UpProbability = 0.9f;

        float scoreLow  = _engine.Score(features, lowUp);
        float scoreHigh = _engine.Score(features, highUp);

        Assert.True(scoreHigh > scoreLow,
            $"Higher up probability should yield higher score. Got {scoreHigh} vs {scoreLow}");
    }

    [Fact]
    public void Score_Decreases_With_Higher_CrashProbability()
    {
        var features = NeutralFeatures();

        var lowCrash  = NeutralPrediction(); lowCrash.CrashProbability  = 0.1f;
        var highCrash = NeutralPrediction(); highCrash.CrashProbability = 0.8f;

        float scoreLow  = _engine.Score(features, lowCrash);
        float scoreHigh = _engine.Score(features, highCrash);

        Assert.True(scoreLow > scoreHigh,
            $"Higher crash probability should yield lower score. Got {scoreLow} vs {scoreHigh}");
    }

    [Fact]
    public void Score_Is_Higher_In_Seasonally_Strong_Months()
    {
        var weak   = NeutralFeatures(); weak.IsSeasonallyStrong   = 0f;
        var strong = NeutralFeatures(); strong.IsSeasonallyStrong = 1f;
        var pred   = NeutralPrediction();

        float scoreWeak   = _engine.Score(weak,   pred);
        float scoreStrong = _engine.Score(strong, pred);

        Assert.True(scoreStrong > scoreWeak,
            $"Seasonally strong month should yield higher score. Got {scoreStrong} vs {scoreWeak}");
    }

    [Fact]
    public void Score_Decreases_With_Higher_Volatility()
    {
        var low  = NeutralFeatures(); low.ReturnStdDev20d  = 0.005f;
        var high = NeutralFeatures(); high.ReturnStdDev20d = 0.10f;
        var pred = NeutralPrediction();

        float scoreLow  = _engine.Score(low,  pred);
        float scoreHigh = _engine.Score(high, pred);

        Assert.True(scoreLow > scoreHigh,
            $"Higher volatility should yield lower score. Got {scoreLow} vs {scoreHigh}");
    }
}
