namespace GoldAI.Domain.Models;

/// <summary>
/// Represents a single daily price record for a financial asset.
/// </summary>
public class AssetPrice
{
    public int Id { get; set; }
    public AssetType Asset { get; set; }
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
