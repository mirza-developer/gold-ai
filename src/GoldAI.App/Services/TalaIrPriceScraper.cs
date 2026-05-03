using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace GoldAI.App.Services;

/// <summary>
/// Scrapes daily asset prices from tala.ir website.
/// The site provides Gold price (in Rials), Silver price (in Rials), and USD price (in Rials).
/// </summary>
public class TalaIrPriceScraper : IPriceScraperService
{
    private const string TalaIrUrl = "https://www.tala.ir/";
    private readonly ILogger<TalaIrPriceScraper> _logger;
    private readonly HttpClient _httpClient;

    public TalaIrPriceScraper(ILogger<TalaIrPriceScraper> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AssetPrice>> ScrapeCurrentPricesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching prices from {Url}", TalaIrUrl);

        try
        {
            var html = await _httpClient.GetStringAsync(TalaIrUrl, ct);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var today = DateTime.UtcNow.Date;
            var prices = new List<AssetPrice>();

            // Extract Gold price (in Rials per gram)
            var goldPrice = ExtractPrice(doc, "gold", "Gold");
            if (goldPrice.HasValue)
            {
                prices.Add(new AssetPrice
                {
                    Asset = AssetType.Gold,
                    Date = today,
                    Open = goldPrice.Value,
                    High = goldPrice.Value,
                    Low = goldPrice.Value,
                    Close = goldPrice.Value,
                    Volume = 0m // Volume not available from scraping
                });
                _logger.LogInformation("Gold price: {Price} Rials", goldPrice.Value);
            }

            // Extract Silver price (in Rials per ounce)
            var silverPrice = ExtractPrice(doc, "silver", "Silver");
            if (silverPrice.HasValue)
            {
                prices.Add(new AssetPrice
                {
                    Asset = AssetType.Silver,
                    Date = today,
                    Open = silverPrice.Value,
                    High = silverPrice.Value,
                    Low = silverPrice.Value,
                    Close = silverPrice.Value,
                    Volume = 0m
                });
                _logger.LogInformation("Silver price: {Price} Rials", silverPrice.Value);
            }

            // Extract USD price (in Rials)
            var usdPrice = ExtractPrice(doc, "USDT_IRT", "USD");
            if (usdPrice.HasValue)
            {
                prices.Add(new AssetPrice
                {
                    Asset = AssetType.USD,
                    Date = today,
                    Open = usdPrice.Value,
                    High = usdPrice.Value,
                    Low = usdPrice.Value,
                    Close = usdPrice.Value,
                    Volume = 0m
                });
                _logger.LogInformation("USD price: {Price} Rials", usdPrice.Value);
            }

            if (prices.Count == 0)
            {
                _logger.LogWarning("No prices could be extracted from {Url}", TalaIrUrl);
            }

            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrape prices from {Url}", TalaIrUrl);
            throw;
        }
    }

    /// <summary>
    /// Extracts price value from a div with the specified ID.
    /// Example HTML structure: &lt;div id="gold"&gt;&lt;span class="price"&gt;18,381,273&lt;/span&gt;&lt;/div&gt;
    /// </summary>
    private decimal? ExtractPrice(HtmlDocument doc, string divId, string assetName)
    {
        try
        {
            // Try multiple possible selectors to find the price
            var selectors = new[]
            {
                $"//div[@id='{divId}']//span[contains(@class, 'price')]",
                $"//div[@id='{divId}']//span",
                $"//div[@id='{divId}']",
                $"//*[@id='{divId}']//span[contains(@class, 'price')]",
                $"//*[@id='{divId}']//text()[normalize-space()]"
            };

            foreach (var selector in selectors)
            {
                var nodes = doc.DocumentNode.SelectNodes(selector);
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var text = node.InnerText.Trim();
                        var price = ParsePriceText(text);
                        if (price.HasValue)
                        {
                            _logger.LogDebug("Found {Asset} price using selector '{Selector}': {Text} -> {Price}",
                                assetName, selector, text, price.Value);
                            return price.Value;
                        }
                    }
                }
            }

            _logger.LogWarning("Could not find {Asset} price with div id '{DivId}'", assetName, divId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting {Asset} price from div id '{DivId}'", assetName, divId);
            return null;
        }
    }

    /// <summary>
    /// Parses a price string like "18,381,273" or "18381273" into a decimal.
    /// </summary>
    private decimal? ParsePriceText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Remove common separators and whitespace
        var cleaned = text.Replace(",", "")
                         .Replace("٬", "") // Persian comma
                         .Replace(" ", "")
                         .Replace("\u00A0", "") // non-breaking space
                         .Trim();

        // Try parsing as decimal
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            // Validate that the value is reasonable (between 1 and 1 billion)
            if (value > 0 && value < 1_000_000_000)
                return value;
        }

        return null;
    }
}
