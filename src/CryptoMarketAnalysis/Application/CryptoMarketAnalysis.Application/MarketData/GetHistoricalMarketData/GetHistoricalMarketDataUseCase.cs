using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Contracts.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Historical;
using CryptoMarketAnalysis.Application.Mapping;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Application.MarketData.GetHistoricalMarketData;

public sealed class GetHistoricalMarketDataUseCase : IGetHistoricalMarketDataUseCase
{
    private readonly ICryptoAssetRepository _cryptoAssetRepository;
    private readonly IMarketDataSourceRepository _marketDataSourceRepository;
    private readonly IMarketDataRepository _marketDataRepository;

    public GetHistoricalMarketDataUseCase(
        ICryptoAssetRepository cryptoAssetRepository,
        IMarketDataSourceRepository marketDataSourceRepository,
        IMarketDataRepository marketDataRepository)
    {
        _cryptoAssetRepository = cryptoAssetRepository
            ?? throw new ArgumentNullException(nameof(cryptoAssetRepository));

        _marketDataSourceRepository = marketDataSourceRepository
            ?? throw new ArgumentNullException(nameof(marketDataSourceRepository));

        _marketDataRepository = marketDataRepository
            ?? throw new ArgumentNullException(nameof(marketDataRepository));
    }

    public async Task<HistoricalMarketDataResponse> ExecuteAsync(
        HistoricalMarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var symbol = new AssetSymbol(request.Symbol);

        Guid? marketDataSourceId = null;
        string? normalizedMarketDataSourceCode = null;

        if (request.MarketDataSourceCode is not null)
        {
            var marketDataSourceCode = new MarketDataSourceCode(request.MarketDataSourceCode);
            normalizedMarketDataSourceCode = marketDataSourceCode.Value;

            MarketDataSource? marketDataSource = await _marketDataSourceRepository.GetByCodeAsync(
                marketDataSourceCode,
                cancellationToken);

            if (marketDataSource is null)
            {
                return CreateEmptyResponse(
                    symbol.Value,
                    normalizedMarketDataSourceCode);
            }

            marketDataSourceId = marketDataSource.Id;
        }

        CryptoAsset? asset = await _cryptoAssetRepository.GetBySymbolAsync(
            symbol,
            cancellationToken);

        if (asset is null)
        {
            return CreateEmptyResponse(
                symbol.Value,
                normalizedMarketDataSourceCode);
        }

        IReadOnlyCollection<MarketDataPoint> points =
            await _marketDataRepository.GetHistoricalAsync(
                asset.Id,
                request.FromUtc,
                request.ToUtc,
                marketDataSourceId,
                cancellationToken);

        MarketDataPointDto[] pointDtos = points
            .OrderBy(point => point.TimestampUtc)
            .Select(point => point.ToDto())
            .ToArray();

        return new HistoricalMarketDataResponse(
            Symbol: symbol.Value,
            MarketDataSourceCode: normalizedMarketDataSourceCode,
            Points: pointDtos);
    }

    private static void ValidateRequest(HistoricalMarketDataRequest request)
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

    private static HistoricalMarketDataResponse CreateEmptyResponse(
        string symbol,
        string? marketDataSourceCode)
    {
        return new HistoricalMarketDataResponse(
            Symbol: symbol,
            MarketDataSourceCode: marketDataSourceCode,
            Points: Array.Empty<MarketDataPointDto>());
    }
}