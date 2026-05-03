using GoldAI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GoldAI.Data;

/// <summary>
/// Entity Framework Core database context for the GoldAI system.
/// Manages AssetPrice records and DailyAnalysisResult JSON documents.
/// </summary>
public class GoldAiDbContext : DbContext
{
    public GoldAiDbContext(DbContextOptions<GoldAiDbContext> options) : base(options) { }

    public DbSet<AssetPrice> AssetPrices => Set<AssetPrice>();
    public DbSet<AnalysisResultEntity> AnalysisResults => Set<AnalysisResultEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}

/// <summary>
/// Lightweight entity that stores daily analysis results as a JSON document.
/// </summary>
public class AnalysisResultEntity
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string ResultJson { get; set; } = string.Empty;
}
