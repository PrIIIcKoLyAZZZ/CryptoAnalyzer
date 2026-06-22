using CryptoMarketAnalysis.Application.Contracts.Reports;

namespace CryptoMarketAnalysis.Application.Abstractions.Reports;

public interface IPdfReportGenerator
{
    Task<byte[]> GenerateAsync(
        MarketAnalysisReportModel report,
        CancellationToken cancellationToken = default);
}