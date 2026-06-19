using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Analytics.Volatility;
using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CryptoMarketAnalysis.Application.Tests.Analytics;

public sealed class VolatilityAnalysisUseCaseTests
{
    private static readonly DateTime FromUtc =
        new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ToUtc =
        new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateVolatility()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(
            assetId,
            new AssetSymbol("BTC"),
            "Bitcoin",
            DateTime.UtcNow);

        MarketDataPoint[] points =
        [
            MarketDataPoint.Create(assetId, sourceId, FromUtc, 100m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(1), 110m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(2), 105m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(3), 120m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository
            .Setup(x => x.GetHistoricalAsync(
                assetId,
                FromUtc,
                ToUtc,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        VolatilityAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            marketDataRepository.Object);

        // Act
        VolatilityAnalysisResponse response =
            await useCase.ExecuteAsync(
                new VolatilityAnalysisRequest(
                    "BTC",
                    FromUtc,
                    ToUtc));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.PointsCount.Should().Be(4);
        response.ReturnsCount.Should().Be(3);

        response.AverageReturnPercent.Should().NotBeNull();
        response.VolatilityPercent.Should().NotBeNull();

        response.VolatilityPercent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNullMetrics_WhenNotEnoughData()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(
            assetId,
            new AssetSymbol("BTC"),
            "Bitcoin",
            DateTime.UtcNow);

        MarketDataPoint[] points =
        [
            MarketDataPoint.Create(assetId, sourceId, FromUtc, 100m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository
            .Setup(x => x.GetHistoricalAsync(
                assetId,
                FromUtc,
                ToUtc,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        VolatilityAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            marketDataRepository.Object);

        // Act
        VolatilityAnalysisResponse response =
            await useCase.ExecuteAsync(
                new VolatilityAnalysisRequest(
                    "BTC",
                    FromUtc,
                    ToUtc));

        // Assert
        response.PointsCount.Should().Be(1);
        response.ReturnsCount.Should().Be(0);

        response.AverageReturnPercent.Should().BeNull();
        response.VolatilityPercent.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnZeroVolatility_ForConstantReturns()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(
            assetId,
            new AssetSymbol("BTC"),
            "Bitcoin",
            DateTime.UtcNow);

        MarketDataPoint[] points =
        [
            MarketDataPoint.Create(assetId, sourceId, FromUtc, 100m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(1), 110m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(2), 121m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(3), 133.1m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository
            .Setup(x => x.GetHistoricalAsync(
                assetId,
                FromUtc,
                ToUtc,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        VolatilityAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            marketDataRepository.Object);

        // Act
        VolatilityAnalysisResponse response =
            await useCase.ExecuteAsync(
                new VolatilityAnalysisRequest(
                    "BTC",
                    FromUtc,
                    ToUtc));

        // Assert
        response.ReturnsCount.Should().Be(3);
        response.VolatilityPercent.Should().BeApproximately(0m, 0.000001m);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyResponse_WhenAssetWasNotFound()
    {
        // Arrange
        Mock<ICryptoAssetRepository> assetRepository = new();

        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CryptoAsset?)null);

        VolatilityAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            Mock.Of<IMarketDataRepository>());

        // Act
        VolatilityAnalysisResponse response =
            await useCase.ExecuteAsync(
                new VolatilityAnalysisRequest(
                    "btc",
                    FromUtc,
                    ToUtc));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.PointsCount.Should().Be(0);
        response.ReturnsCount.Should().Be(0);

        response.AverageReturnPercent.Should().BeNull();
        response.VolatilityPercent.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyResponse_WhenSourceWasNotFound()
    {
        // Arrange
        Mock<IMarketDataSourceRepository> sourceRepository = new();

        sourceRepository
            .Setup(x => x.GetByCodeAsync(
                It.IsAny<MarketDataSourceCode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MarketDataSource?)null);

        VolatilityAnalysisUseCase useCase = new(
            Mock.Of<ICryptoAssetRepository>(),
            sourceRepository.Object,
            Mock.Of<IMarketDataRepository>());

        // Act
        VolatilityAnalysisResponse response =
            await useCase.ExecuteAsync(
                new VolatilityAnalysisRequest(
                    "BTC",
                    FromUtc,
                    ToUtc,
                    " binance "));

        // Assert
        response.Symbol.Should().Be("BTC");
        response.MarketDataSourceCode.Should().Be("BINANCE");

        response.PointsCount.Should().Be(0);
        response.ReturnsCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateAverageReturnCorrectly()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset asset = new(
            assetId,
            new AssetSymbol("BTC"),
            "Bitcoin",
            DateTime.UtcNow);

        MarketDataPoint[] points =
        [
            MarketDataPoint.Create(assetId, sourceId, FromUtc, 100m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(1), 110m),
            MarketDataPoint.Create(assetId, sourceId, FromUtc.AddDays(2), 121m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.IsAny<AssetSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMarketDataRepository> marketDataRepository = new();
        marketDataRepository
            .Setup(x => x.GetHistoricalAsync(
                assetId,
                FromUtc,
                ToUtc,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        VolatilityAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            marketDataRepository.Object);

        // Act
        VolatilityAnalysisResponse response =
            await useCase.ExecuteAsync(
                new VolatilityAnalysisRequest(
                    "BTC",
                    FromUtc,
                    ToUtc));

        // Assert
        response.ReturnsCount.Should().Be(2);
        response.AverageReturnPercent.Should().BeApproximately(10m, 0.000001m);
    }
}