using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CryptoMarketAnalysis.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        CryptoMarketAnalysisDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        await SeedCryptoAssetsAsync(
            dbContext,
            cancellationToken);

        await SeedMarketDataSourcesAsync(
            dbContext,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedCryptoAssetsAsync(
        CryptoMarketAnalysisDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await AddCryptoAssetIfNotExistsAsync(
            dbContext,
            symbol: "BTC",
            name: "Bitcoin",
            cancellationToken);

        await AddCryptoAssetIfNotExistsAsync(
            dbContext,
            symbol: "ETH",
            name: "Ethereum",
            cancellationToken);
    }

    private static async Task AddCryptoAssetIfNotExistsAsync(
        CryptoMarketAnalysisDbContext dbContext,
        string symbol,
        string name,
        CancellationToken cancellationToken)
    {
        var assetSymbol = new AssetSymbol(symbol);

        bool exists = await dbContext.CryptoAssets.AnyAsync(
            cryptoAsset => cryptoAsset.Symbol == assetSymbol,
            cancellationToken);

        if (exists)
            return;

        var cryptoAsset = CryptoAsset.Create(
            assetSymbol.Value,
            name);

        // TODO
        Console.WriteLine($"Seeding asset {cryptoAsset.Symbol.Value} with id {cryptoAsset.Id}");

        await dbContext.CryptoAssets.AddAsync(
            cryptoAsset,
            cancellationToken);
    }

    private static async Task SeedMarketDataSourcesAsync(
        CryptoMarketAnalysisDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await AddMarketDataSourceIfNotExistsAsync(
            dbContext,
            code: "BINANCE",
            name: "Binance",
            cancellationToken);

        await AddMarketDataSourceIfNotExistsAsync(
            dbContext,
            code: "COINGECKO",
            name: "CoinGecko",
            cancellationToken);
    }

    private static async Task AddMarketDataSourceIfNotExistsAsync(
        CryptoMarketAnalysisDbContext dbContext,
        string code,
        string name,
        CancellationToken cancellationToken)
    {
        var marketDataSourceCode = new MarketDataSourceCode(code);

        bool exists = await dbContext.MarketDataSources.AnyAsync(
            marketDataSource => marketDataSource.Code == marketDataSourceCode,
            cancellationToken);

        if (exists)
            return;

        var marketDataSource = MarketDataSource.Create(
            marketDataSourceCode.Value,
            name);

        await dbContext.MarketDataSources.AddAsync(
            marketDataSource,
            cancellationToken);
    }
}