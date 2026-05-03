using GoldAI.Domain.Models;

namespace GoldAI.Domain.Interfaces;

/// <summary>
/// Provides access to historical asset price records.
/// </summary>
public interface IPriceRepository
{
    /// <summary>Returns all available price records for the specified asset, ordered by date ascending.</summary>
    Task<IReadOnlyList<AssetPrice>> GetAllAsync(AssetType asset, CancellationToken ct = default);

    /// <summary>Returns price records for the specified asset within the given date range.</summary>
    Task<IReadOnlyList<AssetPrice>> GetRangeAsync(
        AssetType asset, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Returns the most recent N price records for the specified asset.</summary>
    Task<IReadOnlyList<AssetPrice>> GetLatestAsync(
        AssetType asset, int count, CancellationToken ct = default);

    /// <summary>Saves a new asset price record to the database.</summary>
    Task SaveAsync(AssetPrice price, CancellationToken ct = default);
}
