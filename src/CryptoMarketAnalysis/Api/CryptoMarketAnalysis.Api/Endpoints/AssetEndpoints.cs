using CryptoMarketAnalysis.Application.Contracts.Assets;

namespace CryptoMarketAnalysis.Api.Endpoints;

public static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assets", async (
                IGetCryptoAssetsUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                IReadOnlyCollection<CryptoAssetDto> result =
                    await useCase.ExecuteAsync(cancellationToken);

                return Results.Ok(result);
            })
            .WithName("GetCryptoAssets");

        return app;
    }
}