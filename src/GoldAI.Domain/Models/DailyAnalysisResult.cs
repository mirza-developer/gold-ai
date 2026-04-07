using System.Text.Json;

namespace GoldAI.Domain.Models;

/// <summary>
/// The complete structured output produced by the system for a single trading day.
/// This record is persisted to the database as a JSON analysis result.
/// </summary>
public class DailyAnalysisResult
{
    public int Id { get; set; }
    public DateTime Date { get; set; }

    // ── Prices ────────────────────────────────────────────────────────────────
    public decimal GoldPrice { get; set; }
    public decimal SilverPrice { get; set; }
    public decimal UsdPrice { get; set; }

    // ── Feature snapshots (Layer 1/2/3) ───────────────────────────────────────
    public MarketFeatures? GoldFeatures { get; set; }
    public MarketFeatures? SilverFeatures { get; set; }
    public MarketFeatures? UsdFeatures { get; set; }

    // ── ML predictions ────────────────────────────────────────────────────────
    public MLPrediction? GoldPrediction { get; set; }
    public MLPrediction? SilverPrediction { get; set; }
    public MLPrediction? UsdPrediction { get; set; }

    // ── Risk metrics ──────────────────────────────────────────────────────────
    /// <summary>Portfolio-level risk score (0–1).</summary>
    public float OverallRiskScore { get; set; }

    /// <summary>True when at least one asset has a crash probability above the threshold.</summary>
    public bool CrashWarning { get; set; }

    /// <summary>True when at least one asset is in a high-volatility regime.</summary>
    public bool HighVolatilityWarning { get; set; }

    // ── Portfolio allocation ──────────────────────────────────────────────────
    public PortfolioAllocation? Allocation { get; set; }

    // ── Asset scores (Quant + AI hybrid) ─────────────────────────────────────
    public float GoldScore { get; set; }
    public float SilverScore { get; set; }
    public float UsdScore { get; set; }

    // ── Serialised JSON for database storage ─────────────────────────────────
    /// <summary>Full analysis serialised to JSON for compact database storage.</summary>
    public string ToJson() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}
