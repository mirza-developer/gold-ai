using GoldAI.Domain.Models;

namespace GoldAI.Domain.Interfaces;

/// <summary>
/// Trains the ML.NET model on historical feature/label data and persists it to disk.
/// </summary>
public interface IModelTrainer
{
    /// <summary>
    /// Trains (or retrains) the prediction model using all available historical data.
    /// </summary>
    /// <param name="goldPrices">Historical Gold prices.</param>
    /// <param name="silverPrices">Historical Silver prices.</param>
    /// <param name="usdPrices">Historical USD prices.</param>
    Task TrainAsync(
        IReadOnlyList<AssetPrice> goldPrices,
        IReadOnlyList<AssetPrice> silverPrices,
        IReadOnlyList<AssetPrice> usdPrices,
        CancellationToken ct = default);
}
