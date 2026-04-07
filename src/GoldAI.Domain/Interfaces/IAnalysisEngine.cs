using GoldAI.Domain.Models;

namespace GoldAI.Domain.Interfaces;

/// <summary>
/// Orchestrates the full daily analysis: feature extraction, ML inference,
/// scoring, risk management, and portfolio allocation.
/// </summary>
public interface IAnalysisEngine
{
    /// <summary>
    /// Runs the complete daily analysis pipeline and returns a structured result.
    /// </summary>
    Task<DailyAnalysisResult> RunAsync(
        IReadOnlyList<AssetPrice> goldPrices,
        IReadOnlyList<AssetPrice> silverPrices,
        IReadOnlyList<AssetPrice> usdPrices,
        CancellationToken ct = default);
}
