using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Analytics.Correlation;
using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CryptoMarketAnalysis.Application.Tests.Analytics;

public sealed class CorrelationAnalysisUseCaseTests
{
    private static readonly DateTime FromUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToUtc = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOne_WhenReturnsHavePerfectPositiveCorrelation()
    {
        // Arrange
        var btcId = Guid.NewGuid();
        var ethId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        CryptoAsset btc = new(btcId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        CryptoAsset eth = new(ethId, new AssetSymbol("ETH"), "Ethereum", DateTime.UtcNow);
        MarketDataSource source = new(sourceId, new MarketDataSourceCode("BINANCE"), "Binance", DateTime.UtcNow);

        MarketDataPoint[] btcPoints =
        [
            MarketDataPoint.Create(btcId, sourceId, FromUtc, 100m),
            MarketDataPoint.Create(btcId, sourceId, FromUtc.AddDays(1), 110m),
            MarketDataPoint.Create(btcId, sourceId, FromUtc.AddDays(2), 132m)
        ];

        MarketDataPoint[] ethPoints =
        [
            MarketDataPoint.Create(ethId, sourceId, FromUtc, 200m),
            MarketDataPoint.Create(ethId, sourceId, FromUtc.AddDays(1), 220m),
            MarketDataPoint.Create(ethId, sourceId, FromUtc.AddDays(2), 264m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = CreateAssetRepository(btc, eth);

        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository
            .Setup(x => x.GetByCodeAsync(
                It.Is<MarketDataSourceCode>(c => c.Value == "BINANCE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        Mock<IMarketDataRepository> marketDataRepository = new();
        SetupHistorical(marketDataRepository, btcId, sourceId, btcPoints);
        SetupHistorical(marketDataRepository, ethId, sourceId, ethPoints);

        CorrelationAnalysisUseCase useCase = new(
            assetRepository.Object,
            sourceRepository.Object,
            marketDataRepository.Object);

        // Act
        CorrelationAnalysisResponse response = await useCase.ExecuteAsync(
            new CorrelationAnalysisRequest("BTC", "ETH", FromUtc, ToUtc, "BINANCE"));

        // Assert
        response.BaseSymbol.Should().Be("BTC");
        response.QuoteSymbol.Should().Be("ETH");
        response.MarketDataSourceCode.Should().Be("BINANCE");
        response.BasePointsCount.Should().Be(3);
        response.QuotePointsCount.Should().Be(3);
        response.MatchedReturnsCount.Should().Be(2);
        response.PearsonCorrelation.Should().NotBeNull();
        response.PearsonCorrelation.Value.Should().BeApproximately(1m, 0.000001m);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnMinusOne_WhenReturnsHavePerfectNegativeCorrelation()
    {
        // Arrange
        var btcId = Guid.NewGuid();
        var ethId = Guid.NewGuid();

        CryptoAsset btc = new(btcId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        CryptoAsset eth = new(ethId, new AssetSymbol("ETH"), "Ethereum", DateTime.UtcNow);

        MarketDataPoint[] btcPoints =
        [
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc, 100m),
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc.AddDays(1), 110m),
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc.AddDays(2), 132m)
        ];

        MarketDataPoint[] ethPoints =
        [
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc, 100m),
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc.AddDays(1), 90m),
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc.AddDays(2), 72m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = CreateAssetRepository(btc, eth);

        Mock<IMarketDataRepository> marketDataRepository = new();
        SetupHistorical(marketDataRepository, btcId, null, btcPoints);
        SetupHistorical(marketDataRepository, ethId, null, ethPoints);

        CorrelationAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            marketDataRepository.Object);

        // Act
        CorrelationAnalysisResponse response = await useCase.ExecuteAsync(
            new CorrelationAnalysisRequest("BTC", "ETH", FromUtc, ToUtc));

        // Assert
        response.MatchedReturnsCount.Should().Be(2);
        response.PearsonCorrelation.Should().NotBeNull();
        response.PearsonCorrelation.Value.Should().BeApproximately(-1m, 0.000001m);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNull_WhenMatchedReturnsCountIsLessThanTwo()
    {
        // Arrange
        var btcId = Guid.NewGuid();
        var ethId = Guid.NewGuid();

        CryptoAsset btc = new(btcId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        CryptoAsset eth = new(ethId, new AssetSymbol("ETH"), "Ethereum", DateTime.UtcNow);

        MarketDataPoint[] btcPoints =
        [
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc, 100m),
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc.AddDays(1), 110m)
        ];

        MarketDataPoint[] ethPoints =
        [
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc, 200m),
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc.AddDays(1), 220m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = CreateAssetRepository(btc, eth);

        Mock<IMarketDataRepository> marketDataRepository = new();
        SetupHistorical(marketDataRepository, btcId, null, btcPoints);
        SetupHistorical(marketDataRepository, ethId, null, ethPoints);

        CorrelationAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            marketDataRepository.Object);

        // Act
        CorrelationAnalysisResponse response = await useCase.ExecuteAsync(
            new CorrelationAnalysisRequest("BTC", "ETH", FromUtc, ToUtc));

        // Assert
        response.MatchedReturnsCount.Should().Be(1);
        response.PearsonCorrelation.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNull_WhenOneSeriesHasZeroVariance()
    {
        // Arrange
        var btcId = Guid.NewGuid();
        var ethId = Guid.NewGuid();

        CryptoAsset btc = new(btcId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        CryptoAsset eth = new(ethId, new AssetSymbol("ETH"), "Ethereum", DateTime.UtcNow);

        MarketDataPoint[] btcPoints =
        [
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc, 100m),
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc.AddDays(1), 110m),
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc.AddDays(2), 121m)
        ];

        MarketDataPoint[] ethPoints =
        [
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc, 200m),
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc.AddDays(1), 220m),
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc.AddDays(2), 242m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = CreateAssetRepository(btc, eth);

        Mock<IMarketDataRepository> marketDataRepository = new();
        SetupHistorical(marketDataRepository, btcId, null, btcPoints);
        SetupHistorical(marketDataRepository, ethId, null, ethPoints);

        CorrelationAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            marketDataRepository.Object);

        // Act
        CorrelationAnalysisResponse response = await useCase.ExecuteAsync(
            new CorrelationAnalysisRequest("BTC", "ETH", FromUtc, ToUtc));

        // Assert
        response.MatchedReturnsCount.Should().Be(2);
        response.PearsonCorrelation.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyResponse_WhenBaseAssetWasNotFound()
    {
        // Arrange
        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.Is<AssetSymbol>(s => s.Value == "BTC"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CryptoAsset?)null);

        CorrelationAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            Mock.Of<IMarketDataRepository>());

        // Act
        CorrelationAnalysisResponse response = await useCase.ExecuteAsync(
            new CorrelationAnalysisRequest(" btc ", " eth ", FromUtc, ToUtc));

        // Assert
        response.BaseSymbol.Should().Be("BTC");
        response.QuoteSymbol.Should().Be("ETH");
        response.BasePointsCount.Should().Be(0);
        response.QuotePointsCount.Should().Be(0);
        response.MatchedReturnsCount.Should().Be(0);
        response.PearsonCorrelation.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyResponse_WhenQuoteAssetWasNotFound()
    {
        // Arrange
        CryptoAsset btc = new(Guid.NewGuid(), new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);

        Mock<ICryptoAssetRepository> assetRepository = new();
        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.Is<AssetSymbol>(s => s.Value == "BTC"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(btc);

        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.Is<AssetSymbol>(s => s.Value == "ETH"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CryptoAsset?)null);

        CorrelationAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            Mock.Of<IMarketDataRepository>());

        // Act
        CorrelationAnalysisResponse response = await useCase.ExecuteAsync(
            new CorrelationAnalysisRequest("BTC", "ETH", FromUtc, ToUtc));

        // Assert
        response.BaseSymbol.Should().Be("BTC");
        response.QuoteSymbol.Should().Be("ETH");
        response.PearsonCorrelation.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyResponse_WhenSourceWasNotFound()
    {
        // Arrange
        Mock<IMarketDataSourceRepository> sourceRepository = new();
        sourceRepository
            .Setup(x => x.GetByCodeAsync(
                It.Is<MarketDataSourceCode>(c => c.Value == "BINANCE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MarketDataSource?)null);

        CorrelationAnalysisUseCase useCase = new(
            Mock.Of<ICryptoAssetRepository>(),
            sourceRepository.Object,
            Mock.Of<IMarketDataRepository>());

        // Act
        CorrelationAnalysisResponse response = await useCase.ExecuteAsync(
            new CorrelationAnalysisRequest("BTC", "ETH", FromUtc, ToUtc, " binance "));

        // Assert
        response.MarketDataSourceCode.Should().Be("BINANCE");
        response.BasePointsCount.Should().Be(0);
        response.QuotePointsCount.Should().Be(0);
        response.PearsonCorrelation.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMatchReturnsByTimestamp()
    {
        // Arrange
        var btcId = Guid.NewGuid();
        var ethId = Guid.NewGuid();

        CryptoAsset btc = new(btcId, new AssetSymbol("BTC"), "Bitcoin", DateTime.UtcNow);
        CryptoAsset eth = new(ethId, new AssetSymbol("ETH"), "Ethereum", DateTime.UtcNow);

        MarketDataPoint[] btcPoints =
        [
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc, 100m),
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc.AddDays(1), 110m),
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc.AddDays(2), 121m),
            MarketDataPoint.Create(btcId, Guid.NewGuid(), FromUtc.AddDays(3), 133.1m)
        ];

        MarketDataPoint[] ethPoints =
        [
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc, 200m),
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc.AddDays(2), 220m),
            MarketDataPoint.Create(ethId, Guid.NewGuid(), FromUtc.AddDays(3), 242m)
        ];

        Mock<ICryptoAssetRepository> assetRepository = CreateAssetRepository(btc, eth);

        Mock<IMarketDataRepository> marketDataRepository = new();
        SetupHistorical(marketDataRepository, btcId, null, btcPoints);
        SetupHistorical(marketDataRepository, ethId, null, ethPoints);

        CorrelationAnalysisUseCase useCase = new(
            assetRepository.Object,
            Mock.Of<IMarketDataSourceRepository>(),
            marketDataRepository.Object);

        // Act
        CorrelationAnalysisResponse response = await useCase.ExecuteAsync(
            new CorrelationAnalysisRequest("BTC", "ETH", FromUtc, ToUtc));

        // Assert
        response.BasePointsCount.Should().Be(4);
        response.QuotePointsCount.Should().Be(3);
        response.MatchedReturnsCount.Should().Be(2);
    }

    private static Mock<ICryptoAssetRepository> CreateAssetRepository(
        CryptoAsset baseAsset,
        CryptoAsset quoteAsset)
    {
        Mock<ICryptoAssetRepository> assetRepository = new();

        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.Is<AssetSymbol>(s => s.Value == baseAsset.Symbol.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseAsset);

        assetRepository
            .Setup(x => x.GetBySymbolAsync(
                It.Is<AssetSymbol>(s => s.Value == quoteAsset.Symbol.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(quoteAsset);

        return assetRepository;
    }

    private static void SetupHistorical(
        Mock<IMarketDataRepository> marketDataRepository,
        Guid assetId,
        Guid? sourceId,
        IReadOnlyCollection<MarketDataPoint> points)
    {
        marketDataRepository
            .Setup(x => x.GetHistoricalAsync(
                assetId,
                FromUtc,
                ToUtc,
                sourceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);
    }
}