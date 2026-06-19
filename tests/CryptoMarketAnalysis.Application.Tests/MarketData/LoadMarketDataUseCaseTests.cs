using CryptoMarketAnalysis.Application.Abstractions.MarketData;
using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Contracts.MarketData.External;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;
using CryptoMarketAnalysis.Application.MarketData.LoadMarketData;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CryptoMarketAnalysis.Application.Tests.MarketData;

public sealed class LoadMarketDataUseCaseTests
{
    private static readonly DateTime FromUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToUtc = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_ShouldLoadSingleSymbol_WhenProviderReturnsNewPoints()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        ExternalMarketDataPointDto[] externalPoints =
        [
            new(FromUtc, 100m, null, 1000m),
            new(FromUtc.AddDays(1), 110m, null, 1200m)
        ];

        Mock<IMarketDataProvider> provider = new();
        provider.SetupGet(x => x.SourceCode).Returns("BINANCE");
        provider.Setup(x => x.GetHistoricalAsync("BTC", FromUtc, ToUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalPoints);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.Is<AssetSymbol>(s => s.Value == "BTC"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(
                It.Is<MarketDataSourceCode>(c => c.Value == "BINANCE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.ExistsAsync(
                assetId,
                sourceId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IUnitOfWork> unitOfWork = new();

        var useCase = new LoadMarketDataUseCase(
            [provider.Object],
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object,
            unitOfWork.Object);

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(
                Symbols: ["BTC"],
                FromUtc: FromUtc,
                ToUtc: ToUtc,
                MarketDataSourceCode: "BINANCE"));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Success);
        response.RequestedSymbolsCount.Should().Be(1);
        response.LoadedPointsCount.Should().Be(2);
        response.SkippedDuplicatesCount.Should().Be(0);

        response.Results.Should().ContainSingle();
        response.Results.Single().Symbol.Should().Be("BTC");
        response.Results.Single().MarketDataSourceCode.Should().Be("BINANCE");
        response.Results.Single().Error.Should().BeNull();

        marketDataRepository.Verify(
            x => x.AddRangeAsync(
                It.Is<IReadOnlyCollection<MarketDataPoint>>(points => points.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipDuplicates_WhenPointsAlreadyExist()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        ExternalMarketDataPointDto[] externalPoints =
        [
            new(FromUtc, 100m, null, 1000m),
            new(FromUtc.AddDays(1), 110m, null, 1200m)
        ];

        Mock<IMarketDataProvider> provider = new();
        provider.SetupGet(x => x.SourceCode).Returns("BINANCE");
        provider.Setup(x => x.GetHistoricalAsync("BTC", FromUtc, ToUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalPoints);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(It.IsAny<MarketDataSourceCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.ExistsAsync(
                assetId,
                sourceId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IUnitOfWork> unitOfWork = new();

        var useCase = new LoadMarketDataUseCase(
            [provider.Object],
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object,
            unitOfWork.Object);

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC"], FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Success);
        response.LoadedPointsCount.Should().Be(0);
        response.SkippedDuplicatesCount.Should().Be(2);

        marketDataRepository.Verify(
            x => x.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<MarketDataPoint>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseAllProviders_WhenMarketDataSourceCodeIsNull()
    {
        // Arrange
        var assetId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        MarketDataSource binanceSource = new(Guid.NewGuid(), new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);
        MarketDataSource coinGeckoSource = new(Guid.NewGuid(), new MarketDataSourceCode("COINGECKO"), "CoinGecko", DateTime.UtcNow);

        ExternalMarketDataPointDto[] points =
        [
            new(FromUtc, 100m, null, 1000m)
        ];

        Mock<IMarketDataProvider> binanceProvider = CreateProvider("BINANCE", points);
        Mock<IMarketDataProvider> coinGeckoProvider = CreateProvider("COINGECKO", points);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(
                It.Is<MarketDataSourceCode>(c => c.Value == "BINANCE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(binanceSource);

        sourceRepository.Setup(x => x.GetByCodeAsync(
                It.Is<MarketDataSourceCode>(c => c.Value == "COINGECKO"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coinGeckoSource);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.ExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IUnitOfWork> unitOfWork = new();

        var useCase = new LoadMarketDataUseCase(
            [binanceProvider.Object, coinGeckoProvider.Object],
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object,
            unitOfWork.Object);

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC"], FromUtc, ToUtc, null));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Success);
        response.LoadedPointsCount.Should().Be(2);
        response.Results.Should().HaveCount(2);
        response.Results.Select(x => x.MarketDataSourceCode)
            .Should()
            .BeEquivalentTo(["BINANCE", "COINGECKO"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPartialSuccess_WhenOneProviderFails()
    {
        // Arrange
        var assetId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource binanceSource = new(Guid.NewGuid(), new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);
        MarketDataSource coinGeckoSource = new(Guid.NewGuid(), new MarketDataSourceCode("COINGECKO"), "CoinGecko", DateTime.UtcNow);

        Mock<IMarketDataProvider> successfulProvider = CreateProvider(
            "BINANCE",
            [new ExternalMarketDataPointDto(FromUtc, 100m, null, 1000m)]);

        Mock<IMarketDataProvider> failedProvider = new();
        failedProvider.SetupGet(x => x.SourceCode).Returns("COINGECKO");
        failedProvider.Setup(x => x.GetHistoricalAsync("BTC", FromUtc, ToUtc, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider failed."));

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(
                It.Is<MarketDataSourceCode>(c => c.Value == "BINANCE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(binanceSource);

        sourceRepository.Setup(x => x.GetByCodeAsync(
                It.Is<MarketDataSourceCode>(c => c.Value == "COINGECKO"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coinGeckoSource);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.ExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IUnitOfWork> unitOfWork = new();

        var useCase = new LoadMarketDataUseCase(
            [successfulProvider.Object, failedProvider.Object],
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object,
            unitOfWork.Object);

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC"], FromUtc, ToUtc, null));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.PartialSuccess);
        response.LoadedPointsCount.Should().Be(1);
        response.Results.Should().Contain(x => x.Error == "Provider failed.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailed_WhenAllProvidersFail()
    {
        // Arrange
        CryptoAsset asset = new(Guid.NewGuid(), new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(Guid.NewGuid(), new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        Mock<IMarketDataProvider> provider = new();
        provider.SetupGet(x => x.SourceCode).Returns("BINANCE");
        provider.Setup(x => x.GetHistoricalAsync("BTC", FromUtc, ToUtc, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Binance failed."));

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(It.IsAny<MarketDataSourceCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        Mock<IMarketDataRepository> marketDataRepository = new();
        Mock<IUnitOfWork> unitOfWork = new();

        var useCase = new LoadMarketDataUseCase(
            [provider.Object],
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object,
            unitOfWork.Object);

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC"], FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Failed);
        response.LoadedPointsCount.Should().Be(0);
        response.Results.Single().Error.Should().Be("Binance failed.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSuppressOperationCanceledException()
    {
        // Arrange
        CryptoAsset asset = new(Guid.NewGuid(), new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(Guid.NewGuid(), new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        Mock<IMarketDataProvider> provider = new();
        provider.SetupGet(x => x.SourceCode).Returns("BINANCE");
        provider.Setup(x => x.GetHistoricalAsync("BTC", FromUtc, ToUtc, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(It.IsAny<MarketDataSourceCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var useCase = new LoadMarketDataUseCase(
            [provider.Object],
            assetRepository.Object,
            sourceRepository.Object,
            Mock.Of<IMarketDataRepository>(),
            Mock.Of<IUnitOfWork>());

        // Act
        Func<Task> act = async () => await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC"], FromUtc, ToUtc, "BINANCE"));

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenProviderReturnsNonUtcPoint()
    {
        // Arrange
        CryptoAsset asset = new(Guid.NewGuid(), new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(Guid.NewGuid(), new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        DateTime unspecifiedTimestamp = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);

        Mock<IMarketDataProvider> provider = CreateProvider(
            "BINANCE",
            [new ExternalMarketDataPointDto(unspecifiedTimestamp, 100m, null, 1000m)]);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(It.IsAny<MarketDataSourceCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        Mock<IMarketDataRepository> marketDataRepository = new();

        var useCase = new LoadMarketDataUseCase(
            [provider.Object],
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object,
            Mock.Of<IUnitOfWork>());

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC"], FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Failed);
        response.LoadedPointsCount.Should().Be(0);
        response.Results.Single().Error.Should().Contain("non-UTC timestamp");

        marketDataRepository.Verify(
            x => x.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<MarketDataPoint>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenAssetWasNotFound()
    {
        // Arrange
        Mock<IMarketDataProvider> provider = new();
        provider.SetupGet(x => x.SourceCode).Returns("BINANCE");

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CryptoAsset?)null);

        var useCase = new LoadMarketDataUseCase(
            [provider.Object],
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            Mock.Of<IMarketDataRepository>(),
            Mock.Of<IUnitOfWork>());

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["btc"], FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Failed);
        response.Results.Single().Symbol.Should().Be("BTC");
        response.Results.Single().Error.Should().Be("Asset 'BTC' was not found.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenMarketDataSourceWasNotFound()
    {
        // Arrange
        CryptoAsset asset = new(Guid.NewGuid(), new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        Mock<IMarketDataProvider> provider = new();
        provider.SetupGet(x => x.SourceCode).Returns("BINANCE");

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(It.IsAny<MarketDataSourceCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MarketDataSource?)null);

        var useCase = new LoadMarketDataUseCase(
            [provider.Object],
            assetRepository.Object,
            sourceRepository.Object,
            Mock.Of<IMarketDataRepository>(),
            Mock.Of<IUnitOfWork>());

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC"], FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Failed);
        response.Results.Single().Error.Should().Be("Market data source 'BINANCE' was not found.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLoadTwoSymbols_WhenProviderReturnsNewPoints()
    {
        // Arrange
        var btcAssetId = Guid.NewGuid();
        var ethAssetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset btc = new(btcAssetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        CryptoAsset eth = new(ethAssetId, new AssetSymbol("ETH"), "Ethereum", DateTime.UtcNow);
        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        ExternalMarketDataPointDto[] btcPoints =
        [
            new(FromUtc, 100m, null, 1000m)
        ];

        ExternalMarketDataPointDto[] ethPoints =
        [
            new(FromUtc, 200m, null, 2000m)
        ];

        Mock<IMarketDataProvider> provider = new();
        provider.SetupGet(x => x.SourceCode).Returns("BINANCE");
        provider.Setup(x => x.GetHistoricalAsync("BTC", FromUtc, ToUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(btcPoints);
        provider.Setup(x => x.GetHistoricalAsync("ETH", FromUtc, ToUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ethPoints);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.Is<AssetSymbol>(s => s.Value == "BTC"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(btc);
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.Is<AssetSymbol>(s => s.Value == "ETH"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(eth);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(
                It.IsAny<MarketDataSourceCode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.ExistsAsync(
                It.IsAny<Guid>(),
                sourceId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IUnitOfWork> unitOfWork = new();

        var useCase = new LoadMarketDataUseCase(
        [provider.Object],
        assetRepository.Object,
        sourceRepository.Object,
        marketDataRepository.Object,
        unitOfWork.Object);

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC", "ETH"], FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Success);
        response.RequestedSymbolsCount.Should().Be(2);
        response.LoadedPointsCount.Should().Be(2);
        response.SkippedDuplicatesCount.Should().Be(0);
        response.Results.Should().HaveCount(2);
        response.Results.Should().OnlyContain(result => result.Error == null);

        marketDataRepository.Verify(
            x => x.AddRangeAsync(
                It.Is<IReadOnlyCollection<MarketDataPoint>>(points => points.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
}

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateLoadedAndSkippedCounts_WhenSomePointsAreDuplicates()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        DateTime duplicateTimestampUtc = FromUtc;
        DateTime newTimestampUtc = FromUtc.AddDays(1);

        ExternalMarketDataPointDto[] externalPoints =
        [
            new(duplicateTimestampUtc, 100m, null, 1000m),
            new(newTimestampUtc, 110m, null, 1200m)
        ];

        Mock<IMarketDataProvider> provider = new();
        provider.SetupGet(x => x.SourceCode).Returns("BINANCE");
        provider.Setup(x => x.GetHistoricalAsync("BTC", FromUtc, ToUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalPoints);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(
                It.IsAny<MarketDataSourceCode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.ExistsAsync(
                assetId,
                sourceId,
                duplicateTimestampUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        marketDataRepository.Setup(x => x.ExistsAsync(
                assetId,
                sourceId,
                newTimestampUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IUnitOfWork> unitOfWork = new();

        var useCase = new LoadMarketDataUseCase(
        [provider.Object],
        assetRepository.Object,
        sourceRepository.Object,
        marketDataRepository.Object,
        unitOfWork.Object);

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC"], FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Success);
        response.LoadedPointsCount.Should().Be(1);
        response.SkippedDuplicatesCount.Should().Be(1);

        response.Results.Should().ContainSingle();
        response.Results.Single().LoadedPointsCount.Should().Be(1);
        response.Results.Single().SkippedDuplicatesCount.Should().Be(1);

        marketDataRepository.Verify(
            x => x.AddRangeAsync(
                It.Is<IReadOnlyCollection<MarketDataPoint>>(points =>
                    points.Count == 1 &&
                    points.Single().TimestampUtc == newTimestampUtc),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailed_WhenRequestedProviderWasNotFound()
    {
        // Arrange
        Mock<ICryptoAssetRepository> assetRepository = new();
        Mock<IMarketDataSourceRepository> sourceRepository = new();
        Mock<IMarketDataRepository> marketDataRepository = new();
        Mock<IUnitOfWork> unitOfWork = new();

        Mock<IMarketDataProvider> provider = new();
        provider.SetupGet(x => x.SourceCode).Returns("COINGECKO");

        var useCase = new LoadMarketDataUseCase(
            [provider.Object],
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object,
            unitOfWork.Object);

        // Act
        LoadMarketDataResponse response = await useCase.ExecuteAsync(
            new LoadMarketDataRequest(["BTC"], FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Status.Should().Be(LoadMarketDataStatus.Failed);
        response.RequestedSymbolsCount.Should().Be(1);
        response.LoadedPointsCount.Should().Be(0);
        response.SkippedDuplicatesCount.Should().Be(0);

        response.Results.Should().ContainSingle();
        response.Results.Single().Symbol.Should().Be("BTC");
        response.Results.Single().MarketDataSourceCode.Should().Be("BINANCE");
        response.Results.Single().Error.Should().Be("Market data provider 'BINANCE' was not found.");

        assetRepository.Verify(
            x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        marketDataRepository.Verify(
            x => x.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<MarketDataPoint>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Mock<IMarketDataProvider> CreateProvider(
        string sourceCode,
        IReadOnlyCollection<ExternalMarketDataPointDto> points)
    {
        Mock<IMarketDataProvider> provider = new();

        provider.SetupGet(x => x.SourceCode).Returns(sourceCode);
        provider.Setup(x => x.GetHistoricalAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        return provider;
    }
}