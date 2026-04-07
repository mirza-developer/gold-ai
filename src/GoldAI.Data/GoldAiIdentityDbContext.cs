using GoldAI.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GoldAI.Data;

/// <summary>
/// Extended database context that includes ASP.NET Core Identity for user management.
/// </summary>
public class GoldAiIdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public GoldAiIdentityDbContext(DbContextOptions<GoldAiIdentityDbContext> options) : base(options) { }

    public DbSet<AssetPrice> AssetPrices => Set<AssetPrice>();
    public DbSet<AnalysisResultEntity> AnalysisResults => Set<AnalysisResultEntity>();
    public DbSet<UserCart> UserCarts => Set<UserCart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Important: call base for Identity tables

        modelBuilder.Entity<AssetPrice>(e =>
        {
            e.ToTable("AssetPrices");
            e.HasKey(p => p.Id);
            e.Property(p => p.Asset).HasConversion<string>().HasMaxLength(10);
            e.Property(p => p.Open).HasPrecision(18, 6);
            e.Property(p => p.High).HasPrecision(18, 6);
            e.Property(p => p.Low).HasPrecision(18, 6);
            e.Property(p => p.Close).HasPrecision(18, 6);
            e.Property(p => p.Volume).HasPrecision(18, 6);
            e.HasIndex(p => new { p.Asset, p.Date }).IsUnique();
        });

        modelBuilder.Entity<AnalysisResultEntity>(e =>
        {
            e.ToTable("AnalysisResults");
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Date).IsUnique();
        });

        modelBuilder.Entity<UserCart>(e =>
        {
            e.ToTable("UserCarts");
            e.HasKey(c => c.Id);
            e.Property(c => c.GoldGrams).HasPrecision(18, 6);
            e.Property(c => c.SilverGrams).HasPrecision(18, 6);
            e.Property(c => c.UsdAmount).HasPrecision(18, 6);
            e.HasOne(c => c.User)
             .WithOne(u => u.Cart)
             .HasForeignKey<UserCart>(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
