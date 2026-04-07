using GoldAI.Domain.Models;

namespace GoldAI.Domain.Interfaces;

/// <summary>
/// Service for scraping daily asset prices from external sources.
/// </summary>
public interface IPriceScraperService
{
    /// <summary>
    /// Scrapes current prices for Gold (in Rials), Silver (in Rials), and USD (in Rials).
    /// Returns a list of AssetPrice objects with today's date and scraped values.
    /// </summary>
    Task<IReadOnlyList<AssetPrice>> ScrapeCurrentPricesAsync(CancellationToken ct = default);
}
