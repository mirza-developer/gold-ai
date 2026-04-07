using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using GoldAI.Features;
using GoldAI.ML.Models;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;

namespace GoldAI.ML;

/// <summary>
/// Trains five binary FastTree gradient-boosting models (one per prediction target)
/// using the full historical feature dataset and saves them to disk.
/// </summary>
public class ModelTrainer : IModelTrainer
{
    private readonly MLContext _mlContext;
    private readonly IFeatureCalculator _featureCalculator;
    private readonly string _modelDirectory;

    /// <summary>Labels supported by the trainer, mapped to file names.</summary>
    internal static readonly IReadOnlyDictionary<string, string> ModelFiles =
        new Dictionary<string, string>
        {
            ["Up"]                 = "model_up.zip",
            ["Crash"]              = "model_crash.zip",
            ["TrendContinuation"]  = "model_trend.zip",
            ["MeanReversion"]      = "model_reversion.zip",
            ["VolatilityExpansion"]= "model_volatility.zip",
        };

    public ModelTrainer(
        IFeatureCalculator featureCalculator,
        string modelDirectory = "models")
    {
        _mlContext         = new MLContext(seed: 42);
        _featureCalculator = featureCalculator;
        _modelDirectory    = modelDirectory;
        Directory.CreateDirectory(_modelDirectory);
    }

    /// <inheritdoc />
    public Task TrainAsync(
        IReadOnlyList<AssetPrice> goldPrices,
        IReadOnlyList<AssetPrice> silverPrices,
        IReadOnlyList<AssetPrice> usdPrices,
        CancellationToken ct = default)
    {
        var dataset = BuildDataset(goldPrices, silverPrices, usdPrices);

        if (dataset.Count < 30)
        {
            // Not enough data to train a meaningful model; skip training.
            return Task.CompletedTask;
        }

        TrainAndSave(dataset, "Up");
        TrainAndSave(dataset, "Crash");
        TrainAndSave(dataset, "TrendContinuation");
        TrainAndSave(dataset, "MeanReversion");
        TrainAndSave(dataset, "VolatilityExpansion");

        return Task.CompletedTask;
    }

    // ── Dataset construction ──────────────────────────────────────────────────

    private List<AssetFeatureInput> BuildDataset(
        IReadOnlyList<AssetPrice> goldPrices,
        IReadOnlyList<AssetPrice> silverPrices,
        IReadOnlyList<AssetPrice> usdPrices)
    {
        const int lookForward = 5;   // days ahead used to compute labels
        const int crashThreshold = 5; // percent drop to qualify as a crash

        var records = new List<AssetFeatureInput>();

        // Only train on Gold data for the primary model; Silver and USD are
        // cross-asset inputs to the feature set.
        foreach (var (prices, asset) in new[]
        {
            (goldPrices,   AssetType.Gold),
            (silverPrices, AssetType.Silver),
            (usdPrices,    AssetType.USD)
        })
        {
            int n = prices.Count;
            // Need at least 50 bars of history plus look-forward window
            int startIndex = 50;
            int endIndex   = n - lookForward - 1;

            for (int i = startIndex; i <= endIndex; i++)
            {
                var slice = prices.Take(i + 1).ToList();

                var features = _featureCalculator.Calculate(
                    slice, goldPrices, silverPrices, usdPrices);

                float futureClose   = (float)prices[i + lookForward].Close;
                float currentClose  = (float)prices[i].Close;
                float futureReturn  = currentClose > 0
                    ? (futureClose - currentClose) / currentClose
                    : 0f;

                // Up: future close > current close by more than 0.5 %
                bool labelUp = futureReturn > 0.005f;

                // Crash: future close drops by more than crashThreshold%
                bool labelCrash = futureReturn < -(crashThreshold / 100f);

                // Trend continuation: same sign momentum continues
                bool labelTrend = features.Momentum5d > 0 == labelUp;

                // Mean reversion: price deviated far from SMA20 and then reverts
                bool labelReversion = MathF.Abs(features.DeviationFromSma20) > 0.02f
                                   && (features.DeviationFromSma20 > 0) != labelUp;

                // Volatility expansion: future returns std > current std
                bool labelVolatility = futureReturn != 0 &&
                    MathF.Abs(futureReturn) > features.ReturnStdDev20d * 1.5f;

                records.Add(ToInput(features,
                    labelUp, labelCrash, labelTrend, labelReversion, labelVolatility));
            }
        }

        return records;
    }

    private static AssetFeatureInput ToInput(MarketFeatures f,
        bool up, bool crash, bool trend, bool reversion, bool volatility) =>
        new()
        {
            Momentum5d                = f.Momentum5d,
            Momentum10d               = f.Momentum10d,
            Momentum20d               = f.Momentum20d,
            Atr14d                    = f.Atr14d,
            ReturnStdDev20d           = f.ReturnStdDev20d,
            DeviationFromSma20        = f.DeviationFromSma20,
            DeviationFromSma50        = f.DeviationFromSma50,
            TrendSlope20d             = f.TrendSlope20d,
            SmaCrossRatio             = f.SmaCrossRatio,
            CorrelationWithUsd20d     = f.CorrelationWithUsd20d,
            CorrelationGoldSilver20d  = f.CorrelationGoldSilver20d,
            UsdMomentum5d             = f.UsdMomentum5d,
            SilverMomentum5d          = f.SilverMomentum5d,
            SilverLeadGoldScore       = f.SilverLeadGoldScore,
            GoldSilverRatio20d        = f.GoldSilverRatio20d,
            MonthOfYear               = f.MonthOfYear,
            WeekOfYear                = f.WeekOfYear,
            IsSeasonallyStrong        = f.IsSeasonallyStrong,
            LabelUp                   = up,
            LabelCrash                = crash,
            LabelTrendContinuation    = trend,
            LabelMeanReversion        = reversion,
            LabelVolatilityExpansion  = volatility,
        };

    // ── Label column name → field name mapping ────────────────────────────────

    private static readonly IReadOnlyDictionary<string, string> LabelColumns =
        new Dictionary<string, string>
        {
            ["Up"]                  = nameof(AssetFeatureInput.LabelUp),
            ["Crash"]               = nameof(AssetFeatureInput.LabelCrash),
            ["TrendContinuation"]   = nameof(AssetFeatureInput.LabelTrendContinuation),
            ["MeanReversion"]       = nameof(AssetFeatureInput.LabelMeanReversion),
            ["VolatilityExpansion"]  = nameof(AssetFeatureInput.LabelVolatilityExpansion),
        };

    // Standardised column name used by the FastTree trainer for every model.
    private const string ActiveLabelColumn = "ActiveLabel";

    // ── Training pipeline ─────────────────────────────────────────────────────

    private void TrainAndSave(
        List<AssetFeatureInput> records,
        string modelKey)
    {
        // Each model uses the full record set but reads its own label column.
        IDataView dataView = _mlContext.Data.LoadFromEnumerable(records);

        string[] featureColumns =
        [
            nameof(AssetFeatureInput.Momentum5d),
            nameof(AssetFeatureInput.Momentum10d),
            nameof(AssetFeatureInput.Momentum20d),
            nameof(AssetFeatureInput.Atr14d),
            nameof(AssetFeatureInput.ReturnStdDev20d),
            nameof(AssetFeatureInput.DeviationFromSma20),
            nameof(AssetFeatureInput.DeviationFromSma50),
            nameof(AssetFeatureInput.TrendSlope20d),
            nameof(AssetFeatureInput.SmaCrossRatio),
            nameof(AssetFeatureInput.CorrelationWithUsd20d),
            nameof(AssetFeatureInput.CorrelationGoldSilver20d),
            nameof(AssetFeatureInput.UsdMomentum5d),
            nameof(AssetFeatureInput.SilverMomentum5d),
            nameof(AssetFeatureInput.SilverLeadGoldScore),
            nameof(AssetFeatureInput.GoldSilverRatio20d),
            nameof(AssetFeatureInput.MonthOfYear),
            nameof(AssetFeatureInput.WeekOfYear),
            nameof(AssetFeatureInput.IsSeasonallyStrong),
        ];

        // Copy the model-specific label column to a common ActiveLabel column so that
        // each model trains on its own distinct label rather than a shared one.
        string sourceLabelColumn = LabelColumns[modelKey];

        var pipeline = _mlContext.Transforms
            .CopyColumns(outputColumnName: ActiveLabelColumn, inputColumnName: sourceLabelColumn)
            .Append(_mlContext.Transforms.Concatenate("Features", featureColumns))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: ActiveLabelColumn,
                featureColumnName: "Features",
                numberOfLeaves: 20,
                numberOfTrees: 50,
                minimumExampleCountPerLeaf: 5,
                learningRate: 0.1));

        var model = pipeline.Fit(dataView);

        string path = Path.Combine(_modelDirectory, ModelFiles[modelKey]);
        _mlContext.Model.Save(model, dataView.Schema, path);
    }
}
