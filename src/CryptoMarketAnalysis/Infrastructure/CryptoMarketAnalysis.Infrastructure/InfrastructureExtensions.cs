using CryptoMarketAnalysis.Application.Abstractions.MarketData;
using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Infrastructure.MarketData.Binance;
using CryptoMarketAnalysis.Infrastructure.MarketData.CoinGecko;
using CryptoMarketAnalysis.Infrastructure.Persistence;
using CryptoMarketAnalysis.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CryptoMarketAnalysis.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<CryptoMarketAnalysisDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ICryptoAssetRepository, CryptoAssetRepository>();
        services.AddScoped<IMarketDataSourceRepository, MarketDataSourceRepository>();
        services.AddScoped<IMarketDataRepository, MarketDataRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<CoinGeckoOptions>(
            configuration.GetSection("MarketDataProviders:CoinGecko"));

        services.AddHttpClient<IMarketDataProvider, CoinGeckoMarketDataProvider>();

        services.Configure<BinanceOptions>(
            configuration.GetSection("MarketDataProviders:Binance"));

        services.AddSingleton<BinanceSymbolMapper>();

        services.AddHttpClient<BinanceMarketDataProvider>((serviceProvider, client) =>
        {
            BinanceOptions options = serviceProvider
                .GetRequiredService<IOptions<BinanceOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddScoped<IMarketDataProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<BinanceMarketDataProvider>());

        return services;
    }
}