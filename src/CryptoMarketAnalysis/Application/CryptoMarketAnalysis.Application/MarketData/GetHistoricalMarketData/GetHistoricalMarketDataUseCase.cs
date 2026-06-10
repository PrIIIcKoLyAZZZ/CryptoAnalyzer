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
    private readonly IExchangeRepository _exchangeRepository;
    private readonly IMarketDataRepository _marketDataRepository;

    public GetHistoricalMarketDataUseCase(
        ICryptoAssetRepository cryptoAssetRepository,
        IExchangeRepository exchangeRepository,
        IMarketDataRepository marketDataRepository)
    {
        _cryptoAssetRepository = cryptoAssetRepository
            ?? throw new ArgumentNullException(nameof(cryptoAssetRepository));

        _exchangeRepository = exchangeRepository
            ?? throw new ArgumentNullException(nameof(exchangeRepository));

        _marketDataRepository = marketDataRepository
            ?? throw new ArgumentNullException(nameof(marketDataRepository));
    }

    public async Task<HistoricalMarketDataResponse> ExecuteAsync(
        HistoricalMarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var symbol = new AssetSymbol(request.Symbol);

        Guid? exchangeId = null;
        string? normalizedExchangeCode = null;

        if (request.ExchangeCode is not null)
        {
            var exchangeCode = new ExchangeCode(request.ExchangeCode);
            normalizedExchangeCode = exchangeCode.Value;

            Exchange? exchange = await _exchangeRepository.GetByCodeAsync(
                exchangeCode,
                cancellationToken);

            if (exchange is null)
            {
                return CreateEmptyResponse(
                    symbol.Value,
                    normalizedExchangeCode);
            }

            exchangeId = exchange.Id;
        }

        CryptoAsset? asset = await _cryptoAssetRepository.GetBySymbolAsync(
            symbol,
            cancellationToken);

        if (asset is null)
        {
            return CreateEmptyResponse(
                symbol.Value,
                normalizedExchangeCode);
        }

        IReadOnlyCollection<MarketDataPoint> points =
            await _marketDataRepository.GetHistoricalAsync(
                asset.Id,
                request.FromUtc,
                request.ToUtc,
                exchangeId,
                cancellationToken);

        MarketDataPointDto[] pointDtos = points
            .OrderBy(point => point.TimestampUtc)
            .Select(point => point.ToDto())
            .ToArray();

        return new HistoricalMarketDataResponse(
            Symbol: symbol.Value,
            ExchangeCode: normalizedExchangeCode,
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

        if (request.ExchangeCode is not null && string.IsNullOrWhiteSpace(request.ExchangeCode))
            throw new ArgumentException("Exchange code cannot be empty.", nameof(request));
    }

    private static HistoricalMarketDataResponse CreateEmptyResponse(
        string symbol,
        string? exchangeCode)
    {
        return new HistoricalMarketDataResponse(
            Symbol: symbol,
            ExchangeCode: exchangeCode,
            Points: Array.Empty<MarketDataPointDto>());
    }
}