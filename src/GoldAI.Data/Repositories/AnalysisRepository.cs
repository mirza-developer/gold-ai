using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GoldAI.Data.Repositories;

/// <inheritdoc />
public class AnalysisRepository : IAnalysisRepository
{
    private readonly GoldAiDbContext _db;
    private static readonly JsonSerializerOptions _jsonOptions =
        new() { WriteIndented = false };

    public AnalysisRepository(GoldAiDbContext db) => _db = db;

    public async Task SaveAsync(DailyAnalysisResult result, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(result, _jsonOptions);

        var existing = await _db.AnalysisResults
            .FirstOrDefaultAsync(r => r.Date == result.Date.Date, ct);

        if (existing is null)
        {
            _db.AnalysisResults.Add(new AnalysisResultEntity
            {
                Date = result.Date.Date,
                ResultJson = json
            });
        }
        else
        {
            existing.ResultJson = json;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<DailyAnalysisResult?> GetLatestAsync(CancellationToken ct = default)
    {
        var entity = await _db.AnalysisResults
            .OrderByDescending(r => r.Date)
            .FirstOrDefaultAsync(ct);

        return entity is null
            ? null
            : JsonSerializer.Deserialize<DailyAnalysisResult>(entity.ResultJson);
    }

    public async Task<IReadOnlyList<DailyAnalysisResult>> GetRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var entities = await _db.AnalysisResults
            .Where(r => r.Date >= from.Date && r.Date <= to.Date)
            .OrderBy(r => r.Date)
            .ToListAsync(ct);

        return entities
            .Select(e => JsonSerializer.Deserialize<DailyAnalysisResult>(e.ResultJson)!)
            .ToList();
    }
}
