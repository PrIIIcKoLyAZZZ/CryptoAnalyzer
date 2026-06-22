namespace CryptoMarketAnalysis.Application.Contracts.Reports;

public interface IGenerateMarketAnalysisReportUseCase
{
    Task<GenerateMarketAnalysisReportResponse> ExecuteAsync(
        GenerateMarketAnalysisReportRequest request,
        CancellationToken cancellationToken = default);
}