using GoldAI.Domain.Models;

namespace GoldAI.Domain.Interfaces;

/// <summary>
/// Persists and retrieves daily analysis results.
/// </summary>
public interface IAnalysisRepository
{
    /// <summary>Saves a new daily analysis result to the database.</summary>
    Task SaveAsync(DailyAnalysisResult result, CancellationToken ct = default);

    /// <summary>Returns the most recent analysis result, or null if none exists.</summary>
    Task<DailyAnalysisResult?> GetLatestAsync(CancellationToken ct = default);

    /// <summary>Returns analysis results for the specified date range.</summary>
    Task<IReadOnlyList<DailyAnalysisResult>> GetRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default);
}
