namespace CryptoMarketAnalysis.Application.Contracts.Analytics;

public interface IAnalyzePriceChangeUseCase
{
    Task<PriceChangeAnalysisResponse> ExecuteAsync(
        PriceChangeAnalysisRequest request,
        CancellationToken cancellationToken = default);
}