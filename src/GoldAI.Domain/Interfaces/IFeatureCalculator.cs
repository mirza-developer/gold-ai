using GoldAI.Domain.Models;

namespace GoldAI.Domain.Interfaces;

/// <summary>
/// Computes the full feature vector for a given asset using its price history
/// and the price history of the two companion assets (for cross-asset signals).
/// </summary>
public interface IFeatureCalculator
{
    /// <summary>
    /// Calculates features for <paramref name="asset"/> on the latest available date.
    /// </summary>
    /// <param name="prices">Full price history for the target asset.</param>
    /// <param name="goldPrices">Full Gold price history (used for cross-asset Layer 2 features).</param>
    /// <param name="silverPrices">Full Silver price history (used for cross-asset Layer 2 features).</param>
    /// <param name="usdPrices">Full USD price history (used for cross-asset Layer 2 features).</param>
    /// <returns>Feature vector for the most recent record in <paramref name="prices"/>.</returns>
    MarketFeatures Calculate(
        IReadOnlyList<AssetPrice> prices,
        IReadOnlyList<AssetPrice> goldPrices,
        IReadOnlyList<AssetPrice> silverPrices,
        IReadOnlyList<AssetPrice> usdPrices);
}
