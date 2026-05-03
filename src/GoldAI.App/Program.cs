using GoldAI.Analysis;
using GoldAI.App.Services;
using GoldAI.Data;
using GoldAI.Data.Repositories;
using GoldAI.Domain.Interfaces;
using GoldAI.Domain.Settings;
using GoldAI.Features;
using GoldAI.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        string connectionString = ctx.Configuration.GetConnectionString("GoldAI")
            ?? throw new InvalidOperationException("Connection string 'GoldAI' is required.");

        string modelDirectory = ctx.Configuration["ModelDirectory"] ?? "models";

        // Database
        services.AddDbContext<GoldAiDbContext>(opts =>
            opts.UseSqlServer(connectionString));

        // Repositories
        services.AddScoped<IPriceRepository>(sp => 
            new PriceRepository(sp.GetRequiredService<GoldAiDbContext>()));
        services.AddScoped<IAnalysisRepository>(sp =>
            new AnalysisRepository(sp.GetRequiredService<GoldAiDbContext>()));

        // Price scraper: TalaIrPriceScraper fetches gold, silver and USD from tala.ir.
        // Note: NobitexPriceFetcher is available for USD-only Nobitex queries if needed,
        // but Nobitex does not trade precious metals so it cannot supply gold/silver prices.
        services.AddHttpClient<IPriceScraperService, TalaIrPriceScraper>();

        // Prediction accuracy self-correction checker
        services.AddScoped<PredictionAccuracyChecker>();

        // Feature engineering
        services.AddSingleton<IFeatureCalculator, FeatureCalculator>();

        // ML
        services.AddSingleton<IModelTrainer>(sp =>
            new ModelTrainer(sp.GetRequiredService<IFeatureCalculator>(), modelDirectory));
        services.AddSingleton<IModelPredictor>(
            new ModelPredictor(modelDirectory));

        // Analysis
        services.AddScoped<IAnalysisEngine, AnalysisEngine>();

        // Daily runner
        services.AddScoped<DailyRunner>();
    })
    .Build();

// Ensure the database schema exists
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GoldAiDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Run the daily analysis
using (var scope = host.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<DailyRunner>();
    await runner.RunAsync();
}

