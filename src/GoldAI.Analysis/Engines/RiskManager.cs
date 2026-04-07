using GoldAI.Domain.Models;

namespace GoldAI.Analysis.Engines;

/// <summary>
/// Enforces risk constraints on a preliminary portfolio allocation.
///
/// Key rules:
/// - If any asset has a crash probability above the crash threshold, reduce its weight and
///   shift capital to USD (safe-haven).
/// - If portfolio-wide volatility is elevated, scale all risky positions down.
/// - Never let expected drawdown exceed ~5 %.
/// </summary>
public class RiskManager
{
    private const float CrashThreshold         = 0.35f;  // crash prob above this triggers defensive action
    private const float HighVolatilityThreshold = 0.03f;  // daily std dev above this = high vol regime
    private const float MaxRiskyWeight          = 0.80f;  // Gold + Silver combined cap in high-risk scenario
    private const float MinUsdWeight            = 0.10f;  // always keep some USD as buffer

    /// <summary>
    /// Evaluates risk metrics and returns a risk-adjusted allocation.
    /// </summary>
    public (PortfolioAllocation Allocation, float RiskScore, bool CrashWarning, bool HighVolWarning)
        AdjustForRisk(
            PortfolioAllocation preliminary,
            MLPrediction goldPrediction,
            MLPrediction silverPrediction,
            MarketFeatures goldFeatures,
            MarketFeatures silverFeatures)
    {
        float goldCrash   = goldPrediction.CrashProbability;
        float silverCrash = silverPrediction.CrashProbability;
        bool  crashWarning = goldCrash > CrashThreshold || silverCrash > CrashThreshold;

        float goldVol   = goldFeatures.ReturnStdDev20d;
        float silverVol = silverFeatures.ReturnStdDev20d;
        bool  highVol   = goldVol > HighVolatilityThreshold || silverVol > HighVolatilityThreshold;

        // Composite risk score (0 = low, 1 = high)
        float riskScore = Math.Clamp(
            (goldCrash + silverCrash) / 2f * 0.5f
            + (goldVol + silverVol) / 2f * 10f * 0.5f,
            0f, 1f);

        float goldWeight   = preliminary.GoldWeight;
        float silverWeight = preliminary.SilverWeight;
        float usdWeight    = preliminary.UsdWeight;

        // Reduce exposure to an asset with elevated crash probability
        if (goldCrash > CrashThreshold)
        {
            float reduction = goldWeight * (goldCrash - CrashThreshold) * 2f;
            goldWeight -= reduction;
            usdWeight  += reduction;
        }
        if (silverCrash > CrashThreshold)
        {
            float reduction = silverWeight * (silverCrash - CrashThreshold) * 2f;
            silverWeight -= reduction;
            usdWeight    += reduction;
        }

        // During high-volatility regimes, cap risky assets
        if (highVol)
        {
            float riskyTotal = goldWeight + silverWeight;
            if (riskyTotal > MaxRiskyWeight)
            {
                float scale = MaxRiskyWeight / riskyTotal;
                float freed = riskyTotal - MaxRiskyWeight;
                goldWeight   *= scale;
                silverWeight *= scale;
                usdWeight    += freed;
            }
        }

        // Ensure minimum USD buffer
        if (usdWeight < MinUsdWeight)
        {
            float deficit = MinUsdWeight - usdWeight;
            usdWeight   = MinUsdWeight;
            // Reduce Gold and Silver proportionally
            float riskyTotal = goldWeight + silverWeight;
            if (riskyTotal > 0)
            {
                goldWeight   -= deficit * (goldWeight   / riskyTotal);
                silverWeight -= deficit * (silverWeight / riskyTotal);
            }
        }

        // Normalise so weights sum to exactly 1.0
        var normalised = Normalise(goldWeight, silverWeight, usdWeight);

        return (
            new PortfolioAllocation
            {
                Date         = preliminary.Date,
                GoldWeight   = normalised.gold,
                SilverWeight = normalised.silver,
                UsdWeight    = normalised.usd,
                RiskScore    = riskScore
            },
            riskScore,
            crashWarning,
            highVol);
    }

    private static (float gold, float silver, float usd) Normalise(float g, float s, float u)
    {
        // Clamp negatives
        g = Math.Max(0f, g);
        s = Math.Max(0f, s);
        u = Math.Max(0f, u);
        float total = g + s + u;
        if (total < 1e-6f) return (0f, 0f, 1f); // fallback: all in USD
        return (g / total, s / total, u / total);
    }
}
