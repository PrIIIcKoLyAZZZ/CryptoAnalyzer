using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Application.Analytics.Correlation;

public sealed class CorrelationAnalysisUseCase : ICalculateCorrelationUseCase
{
    private readonly ICryptoAssetRepository _cryptoAssetRepository;
    private readonly IMarketDataSourceRepository _marketDataSourceRepository;
    private readonly IMarketDataRepository _marketDataRepository;

    public CorrelationAnalysisUseCase(
        ICryptoAssetRepository cryptoAssetRepository,
        IMarketDataSourceRepository marketDataSourceRepository,
        IMarketDataRepository marketDataRepository)
    {
        _cryptoAssetRepository = cryptoAssetRepository;
        _marketDataSourceRepository = marketDataSourceRepository;
        _marketDataRepository = marketDataRepository;
    }

    public async Task<CorrelationAnalysisResponse> ExecuteAsync(
        CorrelationAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var baseSymbol = new AssetSymbol(request.BaseSymbol);
        var quoteSymbol = new AssetSymbol(request.QuoteSymbol);

        string? normalizedSourceCode = null;
        Guid? sourceId = null;

        if (request.MarketDataSourceCode is not null)
        {
            var sourceCode = new MarketDataSourceCode(request.MarketDataSourceCode);
            normalizedSourceCode = sourceCode.Value;

            MarketDataSource? source = await _marketDataSourceRepository.GetByCodeAsync(
                sourceCode,
                cancellationToken);

            if (source is null)
                return EmptyResponse(request, baseSymbol.Value, quoteSymbol.Value, normalizedSourceCode, 0, 0, 0);

            sourceId = source.Id;
        }

        CryptoAsset? baseAsset = await _cryptoAssetRepository.GetBySymbolAsync(
            baseSymbol,
            cancellationToken);

        if (baseAsset is null)
            return EmptyResponse(request, baseSymbol.Value, quoteSymbol.Value, normalizedSourceCode, 0, 0, 0);

        CryptoAsset? quoteAsset = await _cryptoAssetRepository.GetBySymbolAsync(
            quoteSymbol,
            cancellationToken);

        if (quoteAsset is null)
            return EmptyResponse(request, baseSymbol.Value, quoteSymbol.Value, normalizedSourceCode, 0, 0, 0);

        IReadOnlyCollection<MarketDataPoint> basePoints = await _marketDataRepository.GetHistoricalAsync(
            baseAsset.Id,
            request.FromUtc,
            request.ToUtc,
            sourceId,
            cancellationToken);

        IReadOnlyCollection<MarketDataPoint> quotePoints = await _marketDataRepository.GetHistoricalAsync(
            quoteAsset.Id,
            request.FromUtc,
            request.ToUtc,
            sourceId,
            cancellationToken);

        Dictionary<DateTime, decimal> baseReturns = CalculateReturnsByTimestamp(basePoints);
        Dictionary<DateTime, decimal> quoteReturns = CalculateReturnsByTimestamp(quotePoints);

        DateTime[] matchedTimestamps = baseReturns.Keys
            .Intersect(quoteReturns.Keys)
            .OrderBy(timestamp => timestamp)
            .ToArray();

        decimal[] matchedBaseReturns = matchedTimestamps
            .Select(timestamp => baseReturns[timestamp])
            .ToArray();

        decimal[] matchedQuoteReturns = matchedTimestamps
            .Select(timestamp => quoteReturns[timestamp])
            .ToArray();

        decimal? correlation = matchedTimestamps.Length < 2
            ? null
            : AnalyticsCalculator.CalculatePearsonCorrelation(
                matchedBaseReturns,
                matchedQuoteReturns);

        return new CorrelationAnalysisResponse(
            BaseSymbol: baseSymbol.Value,
            QuoteSymbol: quoteSymbol.Value,
            MarketDataSourceCode: normalizedSourceCode,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            BasePointsCount: basePoints.Count,
            QuotePointsCount: quotePoints.Count,
            MatchedReturnsCount: matchedTimestamps.Length,
            PearsonCorrelation: correlation);
    }

    private static Dictionary<DateTime, decimal> CalculateReturnsByTimestamp(
        IReadOnlyCollection<MarketDataPoint> points)
    {
        MarketDataPoint[] orderedPoints = points
            .OrderBy(point => point.TimestampUtc)
            .ToArray();

        var returns = new Dictionary<DateTime, decimal>();

        for (int i = 1; i < orderedPoints.Length; i++)
        {
            MarketDataPoint previous = orderedPoints[i - 1];
            MarketDataPoint current = orderedPoints[i];

            if (previous.PriceUsd == 0)
                continue;

            decimal returnPercent = (current.PriceUsd - previous.PriceUsd) / previous.PriceUsd * 100m;

            returns[current.TimestampUtc] = returnPercent;
        }

        return returns;
    }

    private static void ValidateRequest(CorrelationAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.BaseSymbol))
            throw new ArgumentException("Base symbol cannot be empty.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.QuoteSymbol))
            throw new ArgumentException("Quote symbol cannot be empty.", nameof(request));

        if (request.FromUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("From date must be in UTC.", nameof(request));

        if (request.ToUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("To date must be in UTC.", nameof(request));

        if (request.FromUtc >= request.ToUtc)
            throw new ArgumentException("From date must be earlier than to date.", nameof(request));

        if (request.MarketDataSourceCode is not null && string.IsNullOrWhiteSpace(request.MarketDataSourceCode))
            throw new ArgumentException("Market data source code cannot be empty.", nameof(request));
    }

    private static CorrelationAnalysisResponse EmptyResponse(
        CorrelationAnalysisRequest request,
        string baseSymbol,
        string quoteSymbol,
        string? marketDataSourceCode,
        int basePointsCount,
        int quotePointsCount,
        int matchedReturnsCount)
    {
        return new CorrelationAnalysisResponse(
            BaseSymbol: baseSymbol,
            QuoteSymbol: quoteSymbol,
            MarketDataSourceCode: marketDataSourceCode,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            BasePointsCount: basePointsCount,
            QuotePointsCount: quotePointsCount,
            MatchedReturnsCount: matchedReturnsCount,
            PearsonCorrelation: null);
    }
}