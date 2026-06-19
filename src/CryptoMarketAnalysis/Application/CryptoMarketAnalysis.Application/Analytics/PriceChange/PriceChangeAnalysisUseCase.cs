using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Application.Analytics.PriceChange;

public sealed class PriceChangeAnalysisUseCase : IAnalyzePriceChangeUseCase
{
    private readonly ICryptoAssetRepository _cryptoAssetRepository;
    private readonly IMarketDataSourceRepository _marketDataSourceRepository;
    private readonly IMarketDataRepository _marketDataRepository;

    public PriceChangeAnalysisUseCase(
        ICryptoAssetRepository cryptoAssetRepository,
        IMarketDataSourceRepository marketDataSourceRepository,
        IMarketDataRepository marketDataRepository)
    {
        _cryptoAssetRepository = cryptoAssetRepository;
        _marketDataSourceRepository = marketDataSourceRepository;
        _marketDataRepository = marketDataRepository;
    }

    public async Task<PriceChangeAnalysisResponse> ExecuteAsync(
        PriceChangeAnalysisRequest request,
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
            {
                return EmptyResponse(request, symbol.Value, normalizedSourceCode, 0);
            }

            sourceId = source.Id;
        }

        CryptoAsset? asset = await _cryptoAssetRepository.GetBySymbolAsync(
            symbol,
            cancellationToken);

        if (asset is null)
        {
            return EmptyResponse(request, symbol.Value, normalizedSourceCode, 0);
        }

        IReadOnlyCollection<MarketDataPoint> points = await _marketDataRepository.GetHistoricalAsync(
            asset.Id,
            request.FromUtc,
            request.ToUtc,
            sourceId,
            cancellationToken);

        MarketDataPoint[] orderedPoints = points
            .OrderBy(point => point.TimestampUtc)
            .ToArray();

        if (orderedPoints.Length < 2)
        {
            return EmptyResponse(
                request,
                symbol.Value,
                normalizedSourceCode,
                orderedPoints.Length);
        }

        decimal startPrice = orderedPoints.First().PriceUsd;
        decimal endPrice = orderedPoints.Last().PriceUsd;
        decimal absoluteChange = endPrice - startPrice;
        decimal percentageChange = absoluteChange / startPrice * 100m;

        return new PriceChangeAnalysisResponse(
            Symbol: symbol.Value,
            MarketDataSourceCode: normalizedSourceCode,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            StartPriceUsd: startPrice,
            EndPriceUsd: endPrice,
            AbsoluteChangeUsd: absoluteChange,
            PercentageChange: percentageChange,
            PointsCount: orderedPoints.Length);
    }

    private static void ValidateRequest(PriceChangeAnalysisRequest request)
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

    private static PriceChangeAnalysisResponse EmptyResponse(
        PriceChangeAnalysisRequest request,
        string symbol,
        string? marketDataSourceCode,
        int pointsCount)
    {
        return new PriceChangeAnalysisResponse(
            Symbol: symbol,
            MarketDataSourceCode: marketDataSourceCode,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            StartPriceUsd: null,
            EndPriceUsd: null,
            AbsoluteChangeUsd: null,
            PercentageChange: null,
            PointsCount: pointsCount);
    }
}