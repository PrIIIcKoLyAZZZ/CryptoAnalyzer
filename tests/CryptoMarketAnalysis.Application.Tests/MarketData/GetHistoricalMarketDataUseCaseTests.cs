using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Contracts.MarketData;
using CryptoMarketAnalysis.Application.MarketData.GetHistoricalMarketData;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CryptoMarketAnalysis.Application.Tests.MarketData;

public sealed class GetHistoricalMarketDataUseCaseTests
{
    private static readonly DateTime FromUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToUtc = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPointsForPeriod()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        MarketDataPoint[] points =
        [
            MarketDataPoint.Create(assetId, sourceId, FromUtc, 100m, null, 1000m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(1), 110m, null, 1200m)
        ];

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
        marketDataRepository.Setup(x => x.GetHistoricalAsync(
                assetId,
                FromUtc,
                ToUtc,
                sourceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        HistoricalMarketDataResponse response = await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest("BTC", FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.MarketDataSourceCode.Should().Be("BINANCE");
        response.Points.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyPoints_WhenAssetWasNotFound()
    {
        // Arrange
        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CryptoAsset?)null);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        Mock<IMarketDataRepository> marketDataRepository = new();

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        HistoricalMarketDataResponse response = await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest("BTC", FromUtc, ToUtc, null));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.MarketDataSourceCode.Should().BeNull();
        response.Points.Should().BeEmpty();

        marketDataRepository.Verify(
            x => x.GetHistoricalAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyPoints_WhenSourceWasNotFound()
    {
        // Arrange
        Mock<ICryptoAssetRepository> assetRepository = new();

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(
                It.Is<MarketDataSourceCode>(c => c.Value == "BINANCE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MarketDataSource?)null);

        Mock<IMarketDataRepository> marketDataRepository = new();

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        HistoricalMarketDataResponse response = await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest("BTC", FromUtc, ToUtc, "binance"));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.MarketDataSourceCode.Should().Be("BINANCE");
        response.Points.Should().BeEmpty();

        assetRepository.Verify(
            x => x.GetBySymbolAsync(
            It.IsAny<AssetSymbol>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        marketDataRepository.Verify(
            x => x.GetHistoricalAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyPoints_WhenRepositoryReturnsNoData()
    {
        // Arrange
        var assetId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.GetHistoricalAsync(
                assetId,
                FromUtc,
                ToUtc,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MarketDataPoint>());

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        HistoricalMarketDataResponse response = await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest("BTC", FromUtc, ToUtc, null));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.MarketDataSourceCode.Should().BeNull();
        response.Points.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterByMarketDataSourceCode_WhenSourceCodeIsProvided()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

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
        marketDataRepository.Setup(x => x.GetHistoricalAsync(
                assetId,
                FromUtc,
                ToUtc,
                sourceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MarketDataPoint>());

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest("BTC", FromUtc, ToUtc, "BINANCE"));

        // Assert
        marketDataRepository.Verify(
            x => x.GetHistoricalAsync(
            assetId,
            FromUtc,
            ToUtc,
            sourceId,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotFilterBySource_WhenSourceCodeIsNull()
    {
        // Arrange
        var assetId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.GetHistoricalAsync(
                assetId,
                FromUtc,
                ToUtc,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MarketDataPoint>());

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest("BTC", FromUtc, ToUtc, null));

        // Assert
        sourceRepository.Verify(
            x => x.GetByCodeAsync(
            It.IsAny<MarketDataSourceCode>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        marketDataRepository.Verify(
            x => x.GetHistoricalAsync(
            assetId,
            FromUtc,
            ToUtc,
            null,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPointsSortedByTimestampUtc()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        MarketDataPoint[] points =
        [
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(2), 120m, null, 1300m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc, 100m, null, 1000m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(1), 110m, null, 1200m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.GetHistoricalAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        HistoricalMarketDataResponse response = await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest("BTC", FromUtc, ToUtc, null));

        // Assert
        response.Points.Select(point => point.TimestampUtc)
            .Should()
            .BeInAscendingOrder();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMapMarketDataPointToDto()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        DateTime timestampUtc = FromUtc.AddDays(1);

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        var point = MarketDataPoint.Create(
            assetId,
            sourceId,
            timestampUtc,
            123.45m,
            1000000m,
            200000m);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.GetHistoricalAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([point]);

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        HistoricalMarketDataResponse response = await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest("BTC", FromUtc, ToUtc, null));

        // Assert
        response.Points.Should().ContainSingle();

        MarketDataPointDto dto = response.Points.Single();
        dto.AssetId.Should().Be(assetId);
        dto.MarketDataSourceId.Should().Be(sourceId);
        dto.TimestampUtc.Should().Be(timestampUtc);
        dto.PriceUsd.Should().Be(123.45m);
        dto.MarketCapUsd.Should().Be(1000000m);
        dto.Volume24hUsd.Should().Be(200000m);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizeSymbol()
    {
        // Arrange
        CryptoAsset asset = new(Guid.NewGuid(), new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(
                It.Is<AssetSymbol>(s => s.Value == "BTC"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.GetHistoricalAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MarketDataPoint>());

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        HistoricalMarketDataResponse response = await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest(" btc ", FromUtc, ToUtc, null));

        // Assert
        response.Symbol.Should().Be("BTC");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizeMarketDataSourceCode()
    {
        // Arrange
        var sourceId = Guid.NewGuid();

        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        Mock<ICryptoAssetRepository> assetRepository = new();

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(
                It.Is<MarketDataSourceCode>(c => c.Value == "BINANCE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        Mock<IMarketDataRepository> marketDataRepository = new();

        var useCase = new GetHistoricalMarketDataUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        HistoricalMarketDataResponse response = await useCase.ExecuteAsync(
            new HistoricalMarketDataRequest("BTC", FromUtc, ToUtc, " binance "));

        // Assert
        response.MarketDataSourceCode.Should().Be("BINANCE");
    }
}