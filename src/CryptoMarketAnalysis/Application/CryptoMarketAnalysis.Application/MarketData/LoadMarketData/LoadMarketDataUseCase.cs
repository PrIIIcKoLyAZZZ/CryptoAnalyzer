using CryptoMarketAnalysis.Application.Abstractions.MarketData;
using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Contracts.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Application.MarketData.LoadMarketData;

public sealed class LoadMarketDataUseCase : ILoadMarketDataUseCase
{
    private readonly List<IMarketDataProvider> _marketDataProviders;
    private readonly ICryptoAssetRepository _cryptoAssetRepository;
    private readonly IMarketDataSourceRepository _marketDataSourceRepository;
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoadMarketDataUseCase(
        IEnumerable<IMarketDataProvider> marketDataProviders,
        ICryptoAssetRepository cryptoAssetRepository,
        IMarketDataSourceRepository marketDataSourceRepository,
        IMarketDataRepository marketDataRepository,
        IUnitOfWork unitOfWork)
    {
        _marketDataProviders = marketDataProviders?.ToList()
            ?? throw new ArgumentNullException(nameof(marketDataProviders));

        _cryptoAssetRepository = cryptoAssetRepository
            ?? throw new ArgumentNullException(nameof(cryptoAssetRepository));

        _marketDataSourceRepository = marketDataSourceRepository
            ?? throw new ArgumentNullException(nameof(marketDataSourceRepository));

        _marketDataRepository = marketDataRepository
            ?? throw new ArgumentNullException(nameof(marketDataRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<LoadMarketDataResponse> ExecuteAsync(
        LoadMarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        IReadOnlyCollection<IMarketDataProvider> providers = SelectProviders(request.MarketDataSourceCode);
        if (providers.Count == 0)
        {
            return CreateFailedProviderResponse(request);
        }

        var results = new List<LoadMarketDataSymbolResult>();

        foreach (string rawSymbol in request.Symbols)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = new AssetSymbol(rawSymbol);

            CryptoAsset? asset = await _cryptoAssetRepository.GetBySymbolAsync(
                symbol,
                cancellationToken);

            if (asset is null)
            {
                AddAssetNotFoundResults(results, symbol.Value, providers);
                continue;
            }

            foreach (IMarketDataProvider provider in providers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                LoadMarketDataSymbolResult result = await LoadForSymbolAndProviderAsync(
                    asset.Id,
                    symbol.Value,
                    provider,
                    request.FromUtc,
                    request.ToUtc,
                    cancellationToken);

                results.Add(result);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LoadMarketDataStatus status = CalculateStatus(results);

        return new LoadMarketDataResponse(
            Status: status,
            RequestedSymbolsCount: request.Symbols.Count,
            LoadedPointsCount: results.Sum(result => result.LoadedPointsCount),
            SkippedDuplicatesCount: results.Sum(result => result.SkippedDuplicatesCount),
            Results: results);
    }

    private static void ValidateRequest(LoadMarketDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Symbols is null || request.Symbols.Count == 0)
            throw new ArgumentException("Symbols collection cannot be empty.", nameof(request));

        if (request.Symbols.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Symbols collection cannot contain empty values.", nameof(request));

        if (request.FromUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("From date must be in UTC.", nameof(request));

        if (request.ToUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("To date must be in UTC.", nameof(request));

        if (request.FromUtc >= request.ToUtc)
            throw new ArgumentException("From date must be earlier than to date.", nameof(request));

        if (request.MarketDataSourceCode is not null && string.IsNullOrWhiteSpace(request.MarketDataSourceCode))
            throw new ArgumentException("Source code cannot be empty.", nameof(request));
    }

    private static void AddAssetNotFoundResults(
        List<LoadMarketDataSymbolResult> results,
        string symbol,
        IReadOnlyCollection<IMarketDataProvider> providers)
    {
        foreach (IMarketDataProvider provider in providers)
        {
            results.Add(new LoadMarketDataSymbolResult(
                Symbol: symbol,
                MarketDataSourceCode: provider.SourceCode,
                LoadedPointsCount: 0,
                SkippedDuplicatesCount: 0,
                Error: $"Asset '{symbol}' was not found."));
        }
    }

    private static string NormalizeSourceCode(string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            throw new InvalidOperationException("Market data provider source code cannot be empty.");

        return sourceCode.Trim().ToUpperInvariant();
    }

    private static LoadMarketDataStatus CalculateStatus(
        List<LoadMarketDataSymbolResult> results)
    {
        if (results.Count == 0)
            return LoadMarketDataStatus.Failed;

        bool hasSuccess = results.Any(result => result.Error is null);
        bool hasError = results.Any(result => result.Error is not null);

        if (hasSuccess && hasError)
            return LoadMarketDataStatus.PartialSuccess;

        if (hasSuccess)
            return LoadMarketDataStatus.Success;

        return LoadMarketDataStatus.Failed;
    }

    private static List<MarketDataPointDto> FilterPointsByPeriod(
        IReadOnlyCollection<MarketDataPointDto> points,
        DateTime fromUtc,
        DateTime toUtc)
    {
        return points
            .Where(point => point.TimestampUtc >= fromUtc && point.TimestampUtc <= toUtc)
            .ToList();
    }

    private static List<MarketDataPointDto> FilterUtcPoints(
        IReadOnlyCollection<MarketDataPointDto> points)
    {
        return points
            .Where(point => point.TimestampUtc.Kind == DateTimeKind.Utc)
            .ToList();
    }

    private static LoadMarketDataResponse CreateFailedProviderResponse(
        LoadMarketDataRequest request)
    {
        IReadOnlyCollection<LoadMarketDataSymbolResult> results = request.Symbols
            .Select(symbol => new LoadMarketDataSymbolResult(
                Symbol: symbol,
                MarketDataSourceCode: request.MarketDataSourceCode ?? string.Empty,
                LoadedPointsCount: 0,
                SkippedDuplicatesCount: 0,
                Error: request.MarketDataSourceCode is not null ? $"Market data provider '{request.MarketDataSourceCode}' was not found." : "No market data providers are registered."))
            .ToList();

        return new LoadMarketDataResponse(
            Status: LoadMarketDataStatus.Failed,
            RequestedSymbolsCount: request.Symbols.Count,
            LoadedPointsCount: 0,
            SkippedDuplicatesCount: 0,
            Results: results);
    }

    private IReadOnlyCollection<IMarketDataProvider> SelectProviders(string? marketDataSourceCode)
    {
        if (_marketDataProviders.Count == 0)
            return Array.Empty<IMarketDataProvider>();

            // throw new InvalidOperationException("No market data providers are registered.");
        if (marketDataSourceCode is null)
            return _marketDataProviders;

        IMarketDataProvider[] providers = _marketDataProviders
            .Where(provider => string.Equals(
                provider.SourceCode,
                marketDataSourceCode,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // if (providers.Length == 0)
           // throw new InvalidOperationException($"Market data provider '{marketDataSourceCode}' is not registered.");
        return providers;
    }

    private async Task<LoadMarketDataSymbolResult> LoadForSymbolAndProviderAsync(
    Guid assetId,
    string symbol,
    IMarketDataProvider provider,
    DateTime fromUtc,
    DateTime toUtc,
    CancellationToken cancellationToken)
{
    string marketDataSourceCode = NormalizeSourceCode(provider.SourceCode);

    MarketDataSource? marketDataSource = await _marketDataSourceRepository.GetByCodeAsync(
        new MarketDataSourceCode(marketDataSourceCode),
        cancellationToken);

    if (marketDataSource is null)
    {
        return new LoadMarketDataSymbolResult(
            Symbol: symbol,
            MarketDataSourceCode: marketDataSourceCode,
            LoadedPointsCount: 0,
            SkippedDuplicatesCount: 0,
            Error: $"Market data source '{marketDataSourceCode}' was not found.");
    }

    try
    {
        IReadOnlyCollection<MarketDataPointDto> points = await provider.GetHistoricalAsync(
            symbol,
            fromUtc,
            toUtc,
            cancellationToken);

        List<MarketDataPointDto> filteredPoints = FilterPointsByPeriod(
            points,
            fromUtc,
            toUtc);

        List<MarketDataPointDto> validUtcPoints = FilterUtcPoints(
            filteredPoints);

        if (validUtcPoints.Count != filteredPoints.Count)
        {
            return new LoadMarketDataSymbolResult(
                Symbol: symbol,
                MarketDataSourceCode: marketDataSourceCode,
                LoadedPointsCount: 0,
                SkippedDuplicatesCount: 0,
                Error: $"Provider {marketDataSourceCode} returned market data point with non-UTC timestamp for symbol {symbol}.");
        }

        return await SaveNewPointsAsync(
            assetId,
            marketDataSource.Id,
            symbol,
            marketDataSourceCode,
            validUtcPoints,
            cancellationToken);
    }
    catch (OperationCanceledException)
    {
        throw;
    }
    catch (Exception exception)
    {
        return new LoadMarketDataSymbolResult(
            Symbol: symbol,
            MarketDataSourceCode: marketDataSourceCode,
            LoadedPointsCount: 0,
            SkippedDuplicatesCount: 0,
            Error: exception.Message);
    }
}

    private async Task<LoadMarketDataSymbolResult> SaveNewPointsAsync(
        Guid assetId,
        Guid marketDataSourceId,
        string symbol,
        string sourceCode,
        IReadOnlyCollection<MarketDataPointDto> points,
        CancellationToken cancellationToken)
    {
        int loadedPointsCount = 0;
        int skippedDuplicatesCount = 0;
        var pointsToAdd = new List<MarketDataPoint>();

        foreach (MarketDataPointDto point in points)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool exists = await _marketDataRepository.ExistsAsync(
                assetId,
                marketDataSourceId,
                point.TimestampUtc,
                cancellationToken);

            if (exists)
            {
                skippedDuplicatesCount++;
                continue;
            }

            var marketDataPoint = MarketDataPoint.Create(
                assetId,
                marketDataSourceId,
                point.TimestampUtc,
                point.PriceUsd,
                point.MarketCapUsd,
                point.Volume24hUsd);

            pointsToAdd.Add(marketDataPoint);
            loadedPointsCount++;
        }

        if (pointsToAdd.Count > 0)
        {
            await _marketDataRepository.AddRangeAsync(
                pointsToAdd,
                cancellationToken);
        }

        return new LoadMarketDataSymbolResult(
            Symbol: symbol,
            MarketDataSourceCode: sourceCode,
            LoadedPointsCount: loadedPointsCount,
            SkippedDuplicatesCount: skippedDuplicatesCount,
            Error: null);
    }
}