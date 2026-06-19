namespace CryptoMarketAnalysis.Application.Contracts.Analytics;

public interface ICalculateVolatilityUseCase
{
    Task<VolatilityAnalysisResponse> ExecuteAsync(
        VolatilityAnalysisRequest request,
        CancellationToken cancellationToken = default);
}