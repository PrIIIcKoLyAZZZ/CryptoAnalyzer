using CryptoMarketAnalysis.Application.Contracts.MarketData.Historical;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;
using CryptoMarketAnalysis.Application.MarketData.LoadMarketData;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoMarketAnalysis.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILoadMarketDataUseCase, LoadMarketDataUseCase>();
        services.AddScoped<IGetHistoricalMarketDataUseCase, IGetHistoricalMarketDataUseCase>();

        return services;
    }
}