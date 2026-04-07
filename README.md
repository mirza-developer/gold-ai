# GoldAI - AI-Assisted Quant Analysis for Gold, Silver, and USD

An intelligent portfolio management system that combines quantitative financial analysis with machine learning to provide data-driven investment recommendations for precious metals (Gold, Silver) and USD.

## 🌟 Features

### 1. **Daily Price Scraping**
- Automated web scraping from [tala.ir](https://www.tala.ir/)
- Fetches Gold (Rials/gram), Silver (Rials/ounce), and USD (Rials) prices
- Stores historical price data in SQL Server database

### 2. **AI-Powered Analysis**
- **3-Layer Feature Engineering**:
  - **Layer 1**: Momentum, Volatility, Mean Reversion, Trend Strength
  - **Layer 2**: Cross-Asset Intelligence (correlations, lead-lag relationships)
  - **Layer 3**: Seasonality Patterns
- **ML.NET Predictions**: Five gradient-boosting models predict:
  - Up Probability
  - Crash Probability
  - Trend Continuation
  - Mean Reversion
  - Volatility Expansion

### 3. **Risk-Controlled Portfolio Recommendations**
- Hybrid scoring (Quant + AI)
- Softmax portfolio allocation
- Risk management with crash/volatility filters
- Target: Maximum ~5% acceptable loss

### 4. **Blazor Web Panel** 🚀
- User authentication & registration
- Personal portfolio/cart management
- Real-time analysis dashboard
- Buy/sell recommendations based on AI analysis
- Track Gold (grams), Silver (grams), and USD holdings

## 📋 Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB, Express, or full version)
- Internet connection (for price scraping)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/mirza-developer/gold-ai.git
cd gold-ai
```

### 2. Configure Database Connection

Edit the connection string in both configuration files:

**For Console App** (`src/GoldAI.App/appsettings.json`):
```json
{
  "ConnectionStrings": {
    "GoldAI": "Server=localhost;Database=GoldAI;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**For Web Panel** (`src/GoldAI.Web/appsettings.json`):
```json
{
  "ConnectionStrings": {
    "GoldAI": "Server=localhost;Database=GoldAI;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

> **Note**: Update the connection string if using Azure SQL, SQL Server Express, or a remote database.

### 3. Build the Solution

```bash
dotnet build GoldAI.slnx
```

### 4. Initialize the Database

The database will be created automatically when you first run either application. Tables include:
- `AssetPrices` - Historical price data
- `AnalysisResults` - Daily analysis outputs (JSON)
- `AspNetUsers` - User accounts
- `UserCarts` - User portfolios

## 📊 Running the Daily Analysis Console App

The console application performs the following tasks daily:

1. **Scrapes current prices** from tala.ir
2. **Stores prices** in the database
3. **Loads historical data** and calculates features
4. **Trains/refreshes ML models** (every 7 days)
5. **Generates analysis** with portfolio recommendations
6. **Saves results** to database

### Run the Console App:

```bash
cd src/GoldAI.App
dotnet run
```

### Sample Output:

```
=== GoldAI Daily Runner — 2026-04-07 ===
Scraping current prices from external source...
Gold price: 18,381,273 Rials
Silver price: 1,245,000 Rials
USD price: 71,500 Rials
New price saved: Gold on 2026-04-07 = 18,381,273 Rials
...
Loading price data from database...
Loaded 120 Gold, 120 Silver, 120 USD price records.
Training ML model on 120 records...
Model training complete.
Running daily analysis...
Analysis result saved.
──────────────────────────────────────────────────
DAILY ANALYSIS SUMMARY — 2026-04-07
──────────────────────────────────────────────────
Prices    │ Gold: 18381273.00  Silver: 1245000.00  USD: 71500.00
Gold ML   │ Up: 62.3%  Crash: 8.1%  Trend: 71.2%
Silver ML │ Up: 58.7%  Crash: 12.4%  Trend: 65.3%
Scores    │ Gold: +0.42  Silver: +0.28  USD: -0.15
Allocation│ Gold: 48%  Silver: 32%  USD: 20%
Risk Score│ 0.18
──────────────────────────────────────────────────
```

### Schedule Daily Runs

**Windows (Task Scheduler)**:
```powershell
schtasks /create /tn "GoldAI Daily Analysis" /tr "C:\path\to\GoldAI.App.exe" /sc daily /st 09:00
```

**Linux/Mac (cron)**:
```bash
crontab -e
# Add: 0 9 * * * cd /path/to/GoldAI.App && dotnet run
```

## 🌐 Running the Blazor Web Panel

The web application provides an interactive interface for users to:
- Register and log in
- View current market analysis
- Manage their portfolio (Gold grams, Silver grams, USD)
- Receive personalized buy/sell recommendations

### Run the Web App:

```bash
cd src/GoldAI.Web
dotnet run
```

Then open your browser to: **https://localhost:5001** (or the URL shown in terminal)

### Default Features:

- **Home Page**: Latest analysis dashboard
- **Login/Register**: User authentication
- **My Cart**: Manage your portfolio
- **Recommendations**: AI-driven suggestions based on current analysis

### User Registration:

1. Navigate to `/Account/Register`
2. Create an account with email and password
3. Log in and start managing your portfolio

## 🏗️ Project Structure

```
gold-ai/
├── src/
│   ├── GoldAI.Domain/          # Domain models & interfaces
│   ├── GoldAI.Data/             # EF Core, repositories, DbContext
│   ├── GoldAI.Features/         # Feature engineering (3 layers)
│   ├── GoldAI.ML/               # ML.NET training & prediction
│   ├── GoldAI.Analysis/         # Scoring, portfolio, risk management
│   ├── GoldAI.App/              # Console application (daily runner)
│   └── GoldAI.Web/              # Blazor Server web panel
├── tests/
│   └── GoldAI.Tests/            # Unit & integration tests
├── GoldAI.slnx                  # Solution file
└── README.md                    # This file
```

## 🔧 Configuration Options

### `appsettings.json` Options:

- **ConnectionStrings:GoldAI**: SQL Server connection string
- **ModelDirectory**: Path to store trained ML models (default: `models/`)

### Environment Variables (Optional):

```bash
export ConnectionStrings__GoldAI="Server=myserver;Database=GoldAI;..."
export ModelDirectory="/custom/path/to/models"
```

## 🧪 Running Tests

```bash
dotnet test tests/GoldAI.Tests/GoldAI.Tests.csproj
```

All 28 tests cover:
- Feature calculation (momentum, volatility, correlations, seasonality)
- Scoring engine
- Portfolio optimizer
- Risk manager
- End-to-end analysis pipeline

## 📈 How It Works

### Daily Workflow:

```
1. Scrape Prices (tala.ir)
        ↓
2. Store in Database
        ↓
3. Load Historical Data
        ↓
4. Calculate Features (Layers 1-3)
        ↓
5. ML Predictions (5 models)
        ↓
6. Hybrid Scoring (Quant + AI)
        ↓
7. Portfolio Optimization
        ↓
8. Risk Management
        ↓
9. Save Analysis Result
```

### Analysis Layers:

**Layer 1 - Market Features**:
- Momentum (5/10/20 day)
- ATR & Standard Deviation
- SMA deviations
- Trend slope

**Layer 2 - Cross-Asset**:
- USD/Silver momentum signals
- Gold-Silver correlation
- Lead-lag relationships
- Gold/Silver ratio

**Layer 3 - Seasonality**:
- Month & week encoding
- Seasonally strong periods (Jan, Feb, Nov, Dec)

## 🔒 Risk Management

The system enforces strict risk controls:
- **Maximum acceptable loss**: ~5%
- **Crash probability filter**: Reduces exposure when crash risk > 35%
- **Volatility filter**: Caps risky assets during high volatility
- **Minimum USD buffer**: Always maintains at least 10% in USD

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `dotnet test`
5. Submit a pull request

## 📝 License

This project is for educational and personal use.

## ⚠️ Disclaimer

This software is provided for educational purposes only. It does not constitute financial advice. Always conduct your own research and consult with qualified financial advisors before making investment decisions. Past performance does not guarantee future results.

## 🐛 Troubleshooting

### Database Connection Issues:

**Error**: `Cannot open database "GoldAI" requested by the login`

**Solution**: Ensure SQL Server is running and the connection string is correct.

```bash
# Check SQL Server status (Windows)
services.msc  # Look for SQL Server service

# Or use LocalDB
sqllocaldb start MSSQLLocalDB
```

### Web Scraping Issues:

**Error**: `Failed to scrape prices from https://www.tala.ir/`

**Solution**: 
- Check internet connection
- Verify the website structure hasn't changed
- The scraper tries multiple CSS selectors automatically

### ML Model Not Found:

**Warning**: `Model not available, using neutral 0.5 probabilities`

**Solution**: This is normal on first run. The system will train models after collecting enough data (minimum 30 records per asset).

## 📧 Support

For issues and questions, please open an issue on GitHub.

---

**Built with ❤️ using .NET 8, ML.NET, Entity Framework Core, and Blazor**
