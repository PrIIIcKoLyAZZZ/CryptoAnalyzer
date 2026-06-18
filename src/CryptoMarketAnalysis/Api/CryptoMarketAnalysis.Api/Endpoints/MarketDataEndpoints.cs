using CryptoMarketAnalysis.Application.Contracts.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Historical;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;

namespace CryptoMarketAnalysis.Api.Endpoints;

public static class MarketDataEndpoints
{
    public static IEndpointRouteBuilder MapMarketDataEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/market-data/history", async (
                string symbol,
                DateTime fromUtc,
                DateTime toUtc,
                string? marketDataSourceCode,
                IGetHistoricalMarketDataUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var request = new HistoricalMarketDataRequest(
                        symbol,
                        fromUtc,
                        toUtc,
                        marketDataSourceCode);

                    HistoricalMarketDataResponse result =
                        await useCase.ExecuteAsync(request, cancellationToken);

                    return Results.Ok(result);
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = exception.Message,
                    });
                }
            })
            .WithName("GetHistoricalMarketData");

        app.MapPost("/api/market-data/load", async (
                LoadMarketDataRequest request,
                ILoadMarketDataUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    LoadMarketDataResponse result =
                        await useCase.ExecuteAsync(request, cancellationToken);

                    return Results.Ok(result);
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = exception.Message,
                    });
                }
            })
            .WithName("LoadMarketData");

        return app;
    }
}