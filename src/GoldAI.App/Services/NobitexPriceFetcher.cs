using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Models;
using GoldAI.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GoldAI.App.Services;

/// <summary>
/// Fetches daily asset prices from the Nobitex cryptocurrency exchange API.
/// <para>
/// Nobitex is a crypto exchange and does not trade physical precious metals.
/// The following mapping is used to supply all three required asset types:
/// </para>
/// <list type="bullet">
///   <item>Gold   → symbol <c>btc-rls</c>  (Bitcoin/RLS  – "digital gold")</item>
///   <item>Silver → symbol <c>eth-rls</c>  (Ethereum/RLS – "digital silver")</item>
///   <item>USD    → symbol <c>usdt-rls</c> (Tether/RLS)</item>
/// </list>
/// API reference: https://apidocs.nobitex.ir/
/// </summary>
public class NobitexPriceFetcher : IPriceScraperService
{
    private const string MarketStatsPath = "/market/stats";

    // srcCurrency values — comma-separated list for a single API call.
    // Nobitex is a crypto exchange; no physical gold/silver pairs exist.
    // BTC is the conventional "digital gold" and ETH the "digital silver".
    private const string SrcCurrencies = "btc,eth,usdt";

    // Map from Nobitex symbol key → AssetType
    // Keys use the "-rls" suffix (Rial) as documented in the Nobitex API.
    private static readonly Dictionary<string, AssetType> SymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["btc-rls"]  = AssetType.Gold,
        ["eth-rls"]  = AssetType.Silver,
        ["usdt-rls"] = AssetType.USD,
    };

    private readonly HttpClient _httpClient;
    private readonly NobitexSettings _settings;
    private readonly ILogger<NobitexPriceFetcher> _logger;

    public NobitexPriceFetcher(
        HttpClient httpClient,
        IOptions<NobitexSettings> options,
        ILogger<NobitexPriceFetcher> logger)
    {
        _httpClient = httpClient;
        _settings   = options.Value;
        _logger     = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(_settings.ApiToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", _settings.ApiToken);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AssetPrice>> ScrapeCurrentPricesAsync(
        CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching prices from Nobitex ({Url}{Path})",
            _settings.BaseUrl, MarketStatsPath);

        try
        {
            var requestPath = $"{MarketStatsPath}?srcCurrency={SrcCurrencies}&dstCurrency=rls";
            var response = await _httpClient.GetAsync(requestPath, ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);
            return ParseMarketStats(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch prices from Nobitex");
            throw;
        }
    }

    // ── Parsing ──────────────────────────────────────────────────────────────

    private IReadOnlyList<AssetPrice> ParseMarketStats(string json)
    {
        var today  = DateTime.UtcNow.Date;
        var prices = new List<AssetPrice>();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("status", out var statusEl) ||
            statusEl.GetString() != "ok")
        {
            _logger.LogWarning("Nobitex API returned non-ok status. Body: {Body}",
                json.Length > 500 ? json[..500] : json);
            return prices;
        }

        if (!root.TryGetProperty("stats", out var statsEl))
        {
            _logger.LogWarning("Nobitex response missing 'stats' field");
            return prices;
        }

        foreach (var (symbol, assetType) in SymbolMap)
        {
            if (!statsEl.TryGetProperty(symbol, out var symbolEl))
            {
                _logger.LogWarning("Symbol '{Symbol}' not found in Nobitex response", symbol);
                continue;
            }

            // Prefer dayClose (canonical daily close) over latest (live last-trade price)
            var close = ParseDecimalField(symbolEl, "dayClose", symbol) ??
                        ParseDecimalField(symbolEl, "latest",   symbol) ??
                        ParseDecimalField(symbolEl, "mark",     symbol);
            var open  = ParseDecimalField(symbolEl, "dayOpen",  symbol) ?? close;
            var high  = ParseDecimalField(symbolEl, "dayHigh",  symbol) ?? close;
            var low   = ParseDecimalField(symbolEl, "dayLow",   symbol) ?? close;
            var vol   = ParseDecimalField(symbolEl, "volumeSrc", symbol) ?? 0m;

            if (close is null)
            {
                _logger.LogWarning("Could not extract price for symbol '{Symbol}'", symbol);
                continue;
            }

            prices.Add(new AssetPrice
            {
                Asset  = assetType,
                Date   = today,
                Open   = open   ?? close.Value,
                High   = high   ?? close.Value,
                Low    = low    ?? close.Value,
                Close  = close.Value,
                Volume = vol,
            });

            _logger.LogInformation("{Asset} ({Symbol}) price: {Price:N0} RLS",
                assetType, symbol, close.Value);
        }

        if (prices.Count == 0)
            _logger.LogWarning("No prices could be extracted from Nobitex response");

        return prices;
    }

    private decimal? ParseDecimalField(JsonElement element, string fieldName, string symbol)
    {
        if (!element.TryGetProperty(fieldName, out var field))
            return null;

        var raw = field.ValueKind == JsonValueKind.String
            ? field.GetString()
            : field.GetRawText();

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Remove thousands separators and whitespace
        var cleaned = raw.Replace(",", "").Replace(" ", "").Trim();

        if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var value)
            && value > 0)
        {
            return value;
        }

        _logger.LogDebug("Could not parse field '{Field}' value '{Raw}' for symbol '{Symbol}'",
            fieldName, raw, symbol);
        return null;
    }
}
