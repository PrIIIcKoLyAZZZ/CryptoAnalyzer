using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Analytics.PriceChange;
using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CryptoMarketAnalysis.Application.Tests.Analytics;

public sealed class PriceChangeAnalysisUseCaseTests
{
    private static readonly DateTime FromUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToUtc = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_ShouldCalculatePriceChange()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        MarketDataPoint[] points =
        [
            MarketDataPoint.Create(assetId, sourceId, FromUtc, 100m, null, null),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(1), 125m, null, null)
        ];

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(It.IsAny<MarketDataSourceCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.GetHistoricalAsync(assetId, FromUtc, ToUtc, sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var useCase = new PriceChangeAnalysisUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        PriceChangeAnalysisResponse response = await useCase.ExecuteAsync(
            new PriceChangeAnalysisRequest("BTC", FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.MarketDataSourceCode.Should().Be("BINANCE");
        response.PointsCount.Should().Be(2);
        response.StartPriceUsd.Should().Be(100m);
        response.EndPriceUsd.Should().Be(125m);
        response.AbsoluteChangeUsd.Should().Be(25m);
        response.PercentageChange.Should().Be(25m);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNullMetrics_WhenNoData()
    {
        // Arrange
        var assetId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.GetHistoricalAsync(assetId, FromUtc, ToUtc, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MarketDataPoint>());

        var useCase = new PriceChangeAnalysisUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        PriceChangeAnalysisResponse response = await useCase.ExecuteAsync(
            new PriceChangeAnalysisRequest("BTC", FromUtc, ToUtc));

        // Assert
        response.PointsCount.Should().Be(0);
        response.StartPriceUsd.Should().BeNull();
        response.EndPriceUsd.Should().BeNull();
        response.AbsoluteChangeUsd.Should().BeNull();
        response.PercentageChange.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNullMetrics_WhenOnlyOnePoint()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        MarketDataPoint[] points =
        [
            MarketDataPoint.Create(assetId, sourceId, FromUtc, 100m, null, null)
        ];

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataSourceRepository> sourceRepository = new();

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository.Setup(x => x.GetHistoricalAsync(assetId, FromUtc, ToUtc, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var useCase = new PriceChangeAnalysisUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        PriceChangeAnalysisResponse response = await useCase.ExecuteAsync(
            new PriceChangeAnalysisRequest("BTC", FromUtc, ToUtc));

        // Assert
        response.PointsCount.Should().Be(1);
        response.StartPriceUsd.Should().BeNull();
        response.EndPriceUsd.Should().BeNull();
        response.AbsoluteChangeUsd.Should().BeNull();
        response.PercentageChange.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyResponse_WhenAssetWasNotFound()
    {
        // Arrange
        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository.Setup(x => x.GetBySymbolAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CryptoAsset?)null);

        var useCase = new PriceChangeAnalysisUseCase(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            Mock.Of<IMarketDataRepository>());

        // Act
        PriceChangeAnalysisResponse response = await useCase.ExecuteAsync(
            new PriceChangeAnalysisRequest("btc", FromUtc, ToUtc));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.PointsCount.Should().Be(0);
        response.StartPriceUsd.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyResponse_WhenSourceWasNotFound()
    {
        // Arrange
        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository.Setup(x => x.GetByCodeAsync(It.IsAny<MarketDataSourceCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MarketDataSource?)null);

        var useCase = new PriceChangeAnalysisUseCase(
            Mock.Of<ICryptoAssetRepository>(),
            sourceRepository.Object,
            Mock.Of<IMarketDataRepository>());

        // Act
        PriceChangeAnalysisResponse response = await useCase.ExecuteAsync(
            new PriceChangeAnalysisRequest("BTC", FromUtc, ToUtc, " binance "));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.MarketDataSourceCode.Should().Be("BINANCE");
        response.PointsCount.Should().Be(0);
        response.StartPriceUsd.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizeSymbolAndMarketDataSourceCode()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(assetId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        MarketDataPoint[] points =
        [
            MarketDataPoint.Create(assetId, sourceId, FromUtc, 100m, null, null),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(1), 110m, null, null)
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
        marketDataRepository.Setup(x => x.GetHistoricalAsync(assetId, FromUtc, ToUtc, sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var useCase = new PriceChangeAnalysisUseCase(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        PriceChangeAnalysisResponse response = await useCase.ExecuteAsync(
            new PriceChangeAnalysisRequest(" btc ", FromUtc, ToUtc, " binance "));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.MarketDataSourceCode.Should().Be("BINANCE");
    }
}