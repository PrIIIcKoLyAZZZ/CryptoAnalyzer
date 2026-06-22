namespace CryptoMarketAnalysis.Application.Contracts.Reports;

public sealed record GenerateMarketAnalysisReportResponse(
    string FileName,
    string ContentType,
    byte[] Content,
    int PointsCount);