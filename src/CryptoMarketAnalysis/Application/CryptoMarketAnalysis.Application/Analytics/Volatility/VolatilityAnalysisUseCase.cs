using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Application.Analytics.Volatility;

public sealed class VolatilityAnalysisUseCase : ICalculateVolatilityUseCase
{
    private readonly ICryptoAssetRepository _cryptoAssetRepository;
    private readonly IMarketDataSourceRepository _marketDataSourceRepository;
    private readonly IMarketDataRepository _marketDataRepository;

    public VolatilityAnalysisUseCase(
        ICryptoAssetRepository cryptoAssetRepository,
        IMarketDataSourceRepository marketDataSourceRepository,
        IMarketDataRepository marketDataRepository)
    {
        _cryptoAssetRepository = cryptoAssetRepository;
        _marketDataSourceRepository = marketDataSourceRepository;
        _marketDataRepository = marketDataRepository;
    }

    public async Task<VolatilityAnalysisResponse> ExecuteAsync(
        VolatilityAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var symbol = new AssetSymbol(request.Symbol);

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
                return EmptyResponse(request, symbol.Value, normalizedSourceCode, 0, 0);

            sourceId = source.Id;
        }

        CryptoAsset? asset = await _cryptoAssetRepository.GetBySymbolAsync(
            symbol,
            cancellationToken);

        if (asset is null)
            return EmptyResponse(request, symbol.Value, normalizedSourceCode, 0, 0);

        IReadOnlyCollection<MarketDataPoint> points = await _marketDataRepository.GetHistoricalAsync(
            asset.Id,
            request.FromUtc,
            request.ToUtc,
            sourceId,
            cancellationToken);

        decimal[] prices = points
            .OrderBy(point => point.TimestampUtc)
            .Select(point => point.PriceUsd)
            .ToArray();

        IReadOnlyCollection<decimal> returns = AnalyticsCalculator.CalculateReturns(prices);

        if (returns.Count < 2)
            return EmptyResponse(request, symbol.Value, normalizedSourceCode, prices.Length, returns.Count);

        decimal? averageReturn = AnalyticsCalculator.CalculateAverage(returns);
        decimal? volatility = AnalyticsCalculator.CalculateSampleStandardDeviation(returns);

        return new VolatilityAnalysisResponse(
            Symbol: symbol.Value,
            MarketDataSourceCode: normalizedSourceCode,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            PointsCount: prices.Length,
            ReturnsCount: returns.Count,
            AverageReturnPercent: averageReturn,
            VolatilityPercent: volatility);
    }

    private static void ValidateRequest(VolatilityAnalysisRequest request)
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
    }

    private static VolatilityAnalysisResponse EmptyResponse(
        VolatilityAnalysisRequest request,
        string symbol,
        string? marketDataSourceCode,
        int pointsCount,
        int returnsCount)
    {
        return new VolatilityAnalysisResponse(
            Symbol: symbol,
            MarketDataSourceCode: marketDataSourceCode,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            PointsCount: pointsCount,
            ReturnsCount: returnsCount,
            AverageReturnPercent: null,
            VolatilityPercent: null);
    }
}