namespace CryptoMarketAnalysis.Application.Contracts.Analytics;

public interface ICalculateCorrelationUseCase
{
    Task<CorrelationAnalysisResponse> ExecuteAsync(
        CorrelationAnalysisRequest request,
        CancellationToken cancellationToken = default);
}