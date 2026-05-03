using Microsoft.AspNetCore.Identity;

namespace GoldAI.Domain.Models;

/// <summary>
/// Application user with ASP.NET Core Identity support.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Full name of the user.</summary>
    public string? FullName { get; set; }

    /// <summary>Date when the user registered.</summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>User's portfolio/cart.</summary>
    public UserCart? Cart { get; set; }
}
