namespace GoldAI.Domain.Models;

/// <summary>
/// Represents a user's portfolio/cart containing Gold, Silver, and USD holdings.
/// </summary>
public class UserCart
{
    public int Id { get; set; }
    
    /// <summary>User ID (foreign key to ApplicationUser).</summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>Navigation property to the user.</summary>
    public ApplicationUser? User { get; set; }
    
    /// <summary>Amount of Gold in grams.</summary>
    public decimal GoldGrams { get; set; }
    
    /// <summary>Amount of Silver in grams.</summary>
    public decimal SilverGrams { get; set; }
    
    /// <summary>Amount of USD.</summary>
    public decimal UsdAmount { get; set; }
    
    /// <summary>Last time the cart was updated.</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Calculates the total value of the cart in Rials based on current prices.
    /// </summary>
    public decimal CalculateTotalValue(decimal goldPricePerGram, decimal silverPricePerGram, decimal usdPriceInRials)
    {
        return (GoldGrams * goldPricePerGram) + 
               (SilverGrams * silverPricePerGram) + 
               (UsdAmount * usdPriceInRials);
    }
}
