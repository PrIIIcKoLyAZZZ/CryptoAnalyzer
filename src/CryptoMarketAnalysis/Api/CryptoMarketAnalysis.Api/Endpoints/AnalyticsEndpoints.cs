using CryptoMarketAnalysis.Application.Contracts.Analytics;

namespace CryptoMarketAnalysis.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analytics/price-change", async (
                string symbol,
                DateTime fromUtc,
                DateTime toUtc,
                string? marketDataSourceCode,
                IAnalyzePriceChangeUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var request = new PriceChangeAnalysisRequest(
                        symbol,
                        fromUtc,
                        toUtc,
                        marketDataSourceCode);

                    PriceChangeAnalysisResponse result =
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
            .WithName("AnalyzePriceChange");

        app.MapGet("/api/analytics/volatility", async (
                string symbol,
                DateTime fromUtc,
                DateTime toUtc,
                string? marketDataSourceCode,
                ICalculateVolatilityUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var request = new VolatilityAnalysisRequest(
                        symbol,
                        fromUtc,
                        toUtc,
                        marketDataSourceCode);

                    VolatilityAnalysisResponse result =
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
            .WithName("CalculateVolatility");

        app.MapGet("/api/analytics/correlation", async (
                string baseSymbol,
                string quoteSymbol,
                DateTime fromUtc,
                DateTime toUtc,
                string? marketDataSourceCode,
                ICalculateCorrelationUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var request = new CorrelationAnalysisRequest(
                        baseSymbol,
                        quoteSymbol,
                        fromUtc,
                        toUtc,
                        marketDataSourceCode);

                    CorrelationAnalysisResponse result =
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
            .WithName("CalculateCorrelation");

        return app;
    }
}