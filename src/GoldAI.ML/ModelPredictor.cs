using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using GoldAI.ML.Models;
using Microsoft.ML;

namespace GoldAI.ML;

/// <summary>
/// Loads the trained ML.NET models from disk and runs inference to produce
/// probabilistic forecasts for a given feature vector.
/// </summary>
public class ModelPredictor : IModelPredictor
{
    private readonly MLContext _mlContext;
    private readonly string _modelDirectory;

    // Lazy-loaded prediction engines; keyed by model name
    private readonly Dictionary<string, PredictionEngine<AssetFeatureInput, BinaryPredictionOutput>>
        _engines = new();

    public ModelPredictor(string modelDirectory = "models")
    {
        _mlContext       = new MLContext(seed: 42);
        _modelDirectory  = modelDirectory;
    }

    /// <inheritdoc />
    public bool IsModelAvailable() =>
        ModelTrainer.ModelFiles.Values.All(file =>
            File.Exists(Path.Combine(_modelDirectory, file)));

    /// <inheritdoc />
    public MLPrediction Predict(MarketFeatures features)
    {
        var input = ToInput(features);

        return new MLPrediction
        {
            Asset                        = features.Asset,
            Date                         = features.Date,
            UpProbability                = GetProbability("Up", input),
            CrashProbability             = GetProbability("Crash", input),
            TrendContinuationProbability = GetProbability("TrendContinuation", input),
            MeanReversionProbability     = GetProbability("MeanReversion", input),
            VolatilityExpansionProbability = GetProbability("VolatilityExpansion", input),
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private float GetProbability(string modelKey, AssetFeatureInput input)
    {
        var engine = GetOrLoadEngine(modelKey);
        if (engine is null) return 0.5f; // neutral fallback when model not available
        var output = engine.Predict(input);
        return output.Probability;
    }

    private PredictionEngine<AssetFeatureInput, BinaryPredictionOutput>? GetOrLoadEngine(
        string modelKey)
    {
        if (_engines.TryGetValue(modelKey, out var existing)) return existing;

        string path = Path.Combine(_modelDirectory, ModelTrainer.ModelFiles[modelKey]);
        if (!File.Exists(path)) return null;

        var model = _mlContext.Model.Load(path, out _);
        var engine = _mlContext.Model.CreatePredictionEngine<AssetFeatureInput, BinaryPredictionOutput>(model);
        _engines[modelKey] = engine;
        return engine;
    }

    private static AssetFeatureInput ToInput(MarketFeatures f) =>
        new()
        {
            Momentum5d               = f.Momentum5d,
            Momentum10d              = f.Momentum10d,
            Momentum20d              = f.Momentum20d,
            Atr14d                   = f.Atr14d,
            ReturnStdDev20d          = f.ReturnStdDev20d,
            DeviationFromSma20       = f.DeviationFromSma20,
            DeviationFromSma50       = f.DeviationFromSma50,
            TrendSlope20d            = f.TrendSlope20d,
            SmaCrossRatio            = f.SmaCrossRatio,
            CorrelationWithUsd20d    = f.CorrelationWithUsd20d,
            CorrelationGoldSilver20d = f.CorrelationGoldSilver20d,
            UsdMomentum5d            = f.UsdMomentum5d,
            SilverMomentum5d         = f.SilverMomentum5d,
            SilverLeadGoldScore      = f.SilverLeadGoldScore,
            GoldSilverRatio20d       = f.GoldSilverRatio20d,
            MonthOfYear              = f.MonthOfYear,
            WeekOfYear               = f.WeekOfYear,
            IsSeasonallyStrong       = f.IsSeasonallyStrong,
        };
}
