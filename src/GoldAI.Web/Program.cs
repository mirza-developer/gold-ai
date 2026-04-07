using GoldAI.Web.Components;
using GoldAI.Data;
using GoldAI.Domain.Models;
using GoldAI.Domain.Interfaces;
using GoldAI.Data.Repositories;
using GoldAI.Features;
using GoldAI.ML;
using GoldAI.Analysis;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("GoldAI")
    ?? throw new InvalidOperationException("Connection string 'GoldAI' is required.");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database
builder.Services.AddDbContext<GoldAiIdentityDbContext>(opts =>
    opts.UseSqlServer(connectionString));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<GoldAiIdentityDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Repositories
builder.Services.AddScoped<IPriceRepository>(sp => 
    new PriceRepository(sp.GetRequiredService<GoldAiIdentityDbContext>()));
builder.Services.AddScoped<IAnalysisRepository>(sp =>
    new AnalysisRepository(sp.GetRequiredService<GoldAiIdentityDbContext>()));

// Feature engineering
builder.Services.AddSingleton<IFeatureCalculator, FeatureCalculator>();

// ML
string modelDirectory = builder.Configuration["ModelDirectory"] ?? "models";
builder.Services.AddSingleton<IModelPredictor>(
    new ModelPredictor(modelDirectory));

// Analysis
builder.Services.AddScoped<IAnalysisEngine, AnalysisEngine>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GoldAiIdentityDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
