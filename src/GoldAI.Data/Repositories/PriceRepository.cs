using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GoldAI.Data.Repositories;

/// <inheritdoc />
public class PriceRepository : IPriceRepository
{
    private readonly DbContext _db;

    public PriceRepository(DbContext db) => _db = db;

    public async Task<IReadOnlyList<AssetPrice>> GetAllAsync(
        AssetType asset, CancellationToken ct = default) =>
        await _db.Set<AssetPrice>()
            .Where(p => p.Asset == asset)
            .OrderBy(p => p.Date)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AssetPrice>> GetRangeAsync(
        AssetType asset, DateTime from, DateTime to, CancellationToken ct = default) =>
        await _db.Set<AssetPrice>()
            .Where(p => p.Asset == asset && p.Date >= from && p.Date <= to)
            .OrderBy(p => p.Date)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AssetPrice>> GetLatestAsync(
        AssetType asset, int count, CancellationToken ct = default) =>
        await _db.Set<AssetPrice>()
            .Where(p => p.Asset == asset)
            .OrderByDescending(p => p.Date)
            .Take(count)
            .OrderBy(p => p.Date)
            .ToListAsync(ct);

    public async Task SaveAsync(AssetPrice price, CancellationToken ct = default)
    {
        _db.Set<AssetPrice>().Add(price);
        await _db.SaveChangesAsync(ct);
    }
}
