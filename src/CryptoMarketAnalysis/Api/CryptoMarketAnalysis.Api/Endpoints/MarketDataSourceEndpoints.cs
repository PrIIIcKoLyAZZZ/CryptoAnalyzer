using CryptoMarketAnalysis.Application.Contracts.MarketDataSources;

namespace CryptoMarketAnalysis.Api.Endpoints;

public static class MarketDataSourceEndpoints
{
    public static IEndpointRouteBuilder MapMarketDataSourceEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/market-data-sources", async (
                IGetMarketDataSourcesUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                IReadOnlyCollection<MarketDataSourceDto> result =
                    await useCase.ExecuteAsync(cancellationToken);

                return Results.Ok(result);
            })
            .WithName("GetMarketDataSources");

        return app;
    }
}