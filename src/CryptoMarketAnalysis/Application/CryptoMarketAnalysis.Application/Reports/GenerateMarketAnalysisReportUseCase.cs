using CryptoMarketAnalysis.Application.Abstractions.Reports;
using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Application.Contracts.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Historical;
using CryptoMarketAnalysis.Application.Contracts.Reports;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Application.Reports;

public sealed class GenerateMarketAnalysisReportUseCase : IGenerateMarketAnalysisReportUseCase
{
    private const string PdfContentType = "application/pdf";

    private readonly IGetHistoricalMarketDataUseCase _getHistoricalMarketDataUseCase;
    private readonly IAnalyzePriceChangeUseCase _analyzePriceChangeUseCase;
    private readonly ICalculateVolatilityUseCase _calculateVolatilityUseCase;
    private readonly ICalculateCorrelationUseCase _calculateCorrelationUseCase;
    private readonly IPdfReportGenerator _pdfReportGenerator;

    public GenerateMarketAnalysisReportUseCase(
        IGetHistoricalMarketDataUseCase getHistoricalMarketDataUseCase,
        IAnalyzePriceChangeUseCase analyzePriceChangeUseCase,
        ICalculateVolatilityUseCase calculateVolatilityUseCase,
        ICalculateCorrelationUseCase calculateCorrelationUseCase,
        IPdfReportGenerator pdfReportGenerator)
    {
        _getHistoricalMarketDataUseCase = getHistoricalMarketDataUseCase;
        _analyzePriceChangeUseCase = analyzePriceChangeUseCase;
        _calculateVolatilityUseCase = calculateVolatilityUseCase;
        _calculateCorrelationUseCase = calculateCorrelationUseCase;
        _pdfReportGenerator = pdfReportGenerator;
    }

    public async Task<GenerateMarketAnalysisReportResponse> ExecuteAsync(
        GenerateMarketAnalysisReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var symbol = new AssetSymbol(request.Symbol);
        string? marketDataSourceCode = request.MarketDataSourceCode is null
            ? null
            : new MarketDataSourceCode(request.MarketDataSourceCode).Value;

        string? correlationSymbol = request.CorrelationSymbol is null
            ? null
            : new AssetSymbol(request.CorrelationSymbol).Value;

        HistoricalMarketDataResponse historicalData =
            await _getHistoricalMarketDataUseCase.ExecuteAsync(
                new HistoricalMarketDataRequest(
                    symbol.Value,
                    request.FromUtc,
                    request.ToUtc,
                    marketDataSourceCode),
                cancellationToken);

        PriceChangeAnalysisResponse priceChange =
            await _analyzePriceChangeUseCase.ExecuteAsync(
                new PriceChangeAnalysisRequest(
                    symbol.Value,
                    request.FromUtc,
                    request.ToUtc,
                    marketDataSourceCode),
                cancellationToken);

        VolatilityAnalysisResponse volatility =
            await _calculateVolatilityUseCase.ExecuteAsync(
                new VolatilityAnalysisRequest(
                    symbol.Value,
                    request.FromUtc,
                    request.ToUtc,
                    marketDataSourceCode),
                cancellationToken);

        CorrelationAnalysisResponse? correlation = null;

        if (correlationSymbol is not null)
        {
            correlation = await _calculateCorrelationUseCase.ExecuteAsync(
                new CorrelationAnalysisRequest(
                    symbol.Value,
                    correlationSymbol,
                    request.FromUtc,
                    request.ToUtc,
                    marketDataSourceCode),
                cancellationToken);
        }

        MarketAnalysisReportPointDto[] points = historicalData.Points
            .OrderBy(point => point.TimestampUtc)
            .Select(point => new MarketAnalysisReportPointDto(
                point.TimestampUtc,
                point.PriceUsd,
                point.MarketCapUsd,
                point.Volume24hUsd))
            .ToArray();

        var report = new MarketAnalysisReportModel(
            Symbol: symbol.Value,
            MarketDataSourceCode: marketDataSourceCode,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            GeneratedAtUtc: DateTime.UtcNow,
            Points: points,
            PriceChange: priceChange,
            Volatility: volatility,
            Correlation: correlation);

        byte[] content = await _pdfReportGenerator.GenerateAsync(
            report,
            cancellationToken);

        return new GenerateMarketAnalysisReportResponse(
            FileName: CreateFileName(symbol.Value, marketDataSourceCode, request.FromUtc, request.ToUtc),
            ContentType: PdfContentType,
            Content: content,
            PointsCount: points.Length);
    }

    private static void ValidateRequest(
        GenerateMarketAnalysisReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Symbol))
            throw new ArgumentException("Symbol cannot be empty.", nameof(request));

        if (request.FromUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("From date must be in UTC.", nameof(request));

        if (request.ToUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("To date must be in UTC.", nameof(request));

        if (request.FromUtc >= request.ToUtc)
            throw new ArgumentException("From date must be earlier than to date.", nameof(request));

        if (request.MarketDataSourceCode is not null && string.IsNullOrWhiteSpace(request.MarketDataSourceCode))
            throw new ArgumentException("Market data source code cannot be empty.", nameof(request));

        if (request.CorrelationSymbol is not null && string.IsNullOrWhiteSpace(request.CorrelationSymbol))
            throw new ArgumentException("Correlation symbol cannot be empty.", nameof(request));
    }

    private static string CreateFileName(
        string symbol,
        string? marketDataSourceCode,
        DateTime fromUtc,
        DateTime toUtc)
    {
        string source = marketDataSourceCode ?? "ALL";
        string from = fromUtc.ToString("yyyyMMdd");
        string to = toUtc.ToString("yyyyMMdd");

        return $"crypto-market-analysis-{symbol}-{source}-{from}-{to}.pdf";
    }
}