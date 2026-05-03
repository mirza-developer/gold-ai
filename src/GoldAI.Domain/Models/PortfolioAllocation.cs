namespace GoldAI.Domain.Models;

/// <summary>
/// Suggested capital allocation across the three tracked assets.
/// Weights always sum to 1.0.
/// </summary>
public class PortfolioAllocation
{
    public DateTime Date { get; set; }
    public float GoldWeight { get; set; }
    public float SilverWeight { get; set; }
    public float UsdWeight { get; set; }

    /// <summary>
    /// Overall risk score for the day (0 = low risk, 1 = high risk).
    /// Drives defensive allocation toward USD when elevated.
    /// </summary>
    public float RiskScore { get; set; }
}
