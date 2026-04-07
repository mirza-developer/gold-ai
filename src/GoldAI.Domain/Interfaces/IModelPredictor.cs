using GoldAI.Domain.Models;

namespace GoldAI.Domain.Interfaces;

/// <summary>
/// Runs inference with the trained ML.NET model to generate probabilistic forecasts.
/// </summary>
public interface IModelPredictor
{
    /// <summary>
    /// Produces ML predictions for the given feature vector.
    /// </summary>
    MLPrediction Predict(MarketFeatures features);

    /// <summary>
    /// Returns true when a trained model is available on disk.
    /// </summary>
    bool IsModelAvailable();
}
