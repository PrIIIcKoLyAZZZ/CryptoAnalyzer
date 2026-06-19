using CryptoMarketAnalysis.Application.Analytics.PriceChange;
using CryptoMarketAnalysis.Application.Assets.GetCryptoAssets;
using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Application.Contracts.Assets;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Historical;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;
using CryptoMarketAnalysis.Application.Contracts.MarketDataSources;
using CryptoMarketAnalysis.Application.MarketData.GetHistoricalMarketData;
using CryptoMarketAnalysis.Application.MarketData.LoadMarketData;
using CryptoMarketAnalysis.Application.MarketDataSources.GetMarketDataSources;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoMarketAnalysis.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILoadMarketDataUseCase, LoadMarketDataUseCase>();
        services.AddScoped<IGetHistoricalMarketDataUseCase, GetHistoricalMarketDataUseCase>();

        services.AddScoped<IGetCryptoAssetsUseCase, GetCryptoAssetsUseCase>();
        services.AddScoped<IGetMarketDataSourcesUseCase, GetMarketDataSourcesUseCase>();

        services.AddScoped<IAnalyzePriceChangeUseCase, PriceChangeAnalysisUseCase>();

        return services;
    }
}