using CryptoMarketAnalysis.Application.Abstractions.Reports;
using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Application.Contracts.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Historical;
using CryptoMarketAnalysis.Application.Contracts.Reports;
using CryptoMarketAnalysis.Application.Reports;
using FluentAssertions;
using Moq;

namespace CryptoMarketAnalysis.Application.Tests.Reports;

public sealed class GenerateMarketAnalysisReportUseCaseTests
{
    private static readonly DateTime FromUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToUtc = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_ShouldGeneratePdfReport()
    {
        // Arrange
        byte[] pdfBytes = [37, 80, 68, 70];

        Mock<IGetHistoricalMarketDataUseCase> historyUseCase = CreateHistoryUseCase(pointsCount: 2);
        Mock<IAnalyzePriceChangeUseCase> priceChangeUseCase = CreatePriceChangeUseCase();
        Mock<ICalculateVolatilityUseCase> volatilityUseCase = CreateVolatilityUseCase();
        Mock<ICalculateCorrelationUseCase> correlationUseCase = CreateCorrelationUseCase();

        Mock<IPdfReportGenerator> pdfGenerator = new();
        pdfGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<MarketAnalysisReportModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pdfBytes);

        GenerateMarketAnalysisReportUseCase useCase = new(
            historyUseCase.Object,
            priceChangeUseCase.Object,
            volatilityUseCase.Object,
            correlationUseCase.Object,
            pdfGenerator.Object);

        // Act
        GenerateMarketAnalysisReportResponse response =
            await useCase.ExecuteAsync(
                new GenerateMarketAnalysisReportRequest(
                    " btc ",
                    FromUtc,
                    ToUtc,
                    " binance ",
                    " eth "));

        // Assert
        response.Content.Should().Equal(pdfBytes);
        response.ContentType.Should().Be("application/pdf");
        response.PointsCount.Should().Be(2);
        response.FileName.Should().Be("crypto-market-analysis-BTC-BINANCE-20260601-20260610.pdf");

        pdfGenerator.Verify(
            x => x.GenerateAsync(
                It.Is<MarketAnalysisReportModel>(report =>
                    report.Symbol == "BTC" &&
                    report.MarketDataSourceCode == "BINANCE" &&
                    report.Points.Count == 2 &&
                    report.Correlation != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateReport_WhenHistoricalDataIsEmpty()
    {
        // Arrange
        byte[] pdfBytes = [37, 80, 68, 70];

        Mock<IGetHistoricalMarketDataUseCase> historyUseCase = CreateHistoryUseCase(pointsCount: 0);
        Mock<IAnalyzePriceChangeUseCase> priceChangeUseCase = CreatePriceChangeUseCase(pointsCount: 0);
        Mock<ICalculateVolatilityUseCase> volatilityUseCase = CreateVolatilityUseCase(pointsCount: 0, returnsCount: 0);
        Mock<ICalculateCorrelationUseCase> correlationUseCase = CreateCorrelationUseCase();

        Mock<IPdfReportGenerator> pdfGenerator = new();
        pdfGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<MarketAnalysisReportModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pdfBytes);

        GenerateMarketAnalysisReportUseCase useCase = new(
            historyUseCase.Object,
            priceChangeUseCase.Object,
            volatilityUseCase.Object,
            correlationUseCase.Object,
            pdfGenerator.Object);

        // Act
        GenerateMarketAnalysisReportResponse response =
            await useCase.ExecuteAsync(
                new GenerateMarketAnalysisReportRequest(
                    "BTC",
                    FromUtc,
                    ToUtc,
                    "BINANCE"));

        // Assert
        response.PointsCount.Should().Be(0);
        response.Content.Should().Equal(pdfBytes);

        pdfGenerator.Verify(
            x => x.GenerateAsync(
                It.Is<MarketAnalysisReportModel>(report => report.Points.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCalculateCorrelation_WhenCorrelationSymbolIsNull()
    {
        // Arrange
        Mock<IGetHistoricalMarketDataUseCase> historyUseCase = CreateHistoryUseCase(pointsCount: 2);
        Mock<IAnalyzePriceChangeUseCase> priceChangeUseCase = CreatePriceChangeUseCase();
        Mock<ICalculateVolatilityUseCase> volatilityUseCase = CreateVolatilityUseCase();
        Mock<ICalculateCorrelationUseCase> correlationUseCase = CreateCorrelationUseCase();

        Mock<IPdfReportGenerator> pdfGenerator = new();
        pdfGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<MarketAnalysisReportModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([37, 80, 68, 70]);

        GenerateMarketAnalysisReportUseCase useCase = new(
            historyUseCase.Object,
            priceChangeUseCase.Object,
            volatilityUseCase.Object,
            correlationUseCase.Object,
            pdfGenerator.Object);

        // Act
        GenerateMarketAnalysisReportResponse response =
            await useCase.ExecuteAsync(
                new GenerateMarketAnalysisReportRequest(
                    "BTC",
                    FromUtc,
                    ToUtc,
                    "BINANCE"));

        // Assert
        response.ContentType.Should().Be("application/pdf");

        correlationUseCase.Verify(
            x => x.ExecuteAsync(
                It.IsAny<CorrelationAnalysisRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        pdfGenerator.Verify(
            x => x.GenerateAsync(
                It.Is<MarketAnalysisReportModel>(report => report.Correlation == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateCorrelation_WhenCorrelationSymbolIsProvided()
    {
        // Arrange
        Mock<IGetHistoricalMarketDataUseCase> historyUseCase = CreateHistoryUseCase(pointsCount: 2);
        Mock<IAnalyzePriceChangeUseCase> priceChangeUseCase = CreatePriceChangeUseCase();
        Mock<ICalculateVolatilityUseCase> volatilityUseCase = CreateVolatilityUseCase();
        Mock<ICalculateCorrelationUseCase> correlationUseCase = CreateCorrelationUseCase();

        Mock<IPdfReportGenerator> pdfGenerator = new();
        pdfGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<MarketAnalysisReportModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([37, 80, 68, 70]);

        GenerateMarketAnalysisReportUseCase useCase = new(
            historyUseCase.Object,
            priceChangeUseCase.Object,
            volatilityUseCase.Object,
            correlationUseCase.Object,
            pdfGenerator.Object);

        // Act
        await useCase.ExecuteAsync(
            new GenerateMarketAnalysisReportRequest(
                "BTC",
                FromUtc,
                ToUtc,
                "BINANCE",
                "ETH"));

        // Assert
        correlationUseCase.Verify(
            x => x.ExecuteAsync(
                It.Is<CorrelationAnalysisRequest>(request =>
                    request.BaseSymbol == "BTC" &&
                    request.QuoteSymbol == "ETH" &&
                    request.MarketDataSourceCode == "BINANCE"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSuppressOperationCanceledException()
    {
        // Arrange
        Mock<IGetHistoricalMarketDataUseCase> historyUseCase = new();
        historyUseCase
            .Setup(x => x.ExecuteAsync(
                It.IsAny<HistoricalMarketDataRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        GenerateMarketAnalysisReportUseCase useCase = new(
            historyUseCase.Object,
            Mock.Of<IAnalyzePriceChangeUseCase>(),
            Mock.Of<ICalculateVolatilityUseCase>(),
            Mock.Of<ICalculateCorrelationUseCase>(),
            Mock.Of<IPdfReportGenerator>());

        // Act
        Func<Task> act = async () =>
            await useCase.ExecuteAsync(
                new GenerateMarketAnalysisReportRequest(
                    "BTC",
                    FromUtc,
                    ToUtc,
                    "BINANCE"));

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static Mock<IGetHistoricalMarketDataUseCase> CreateHistoryUseCase(
        int pointsCount)
    {
        MarketDataPointDto[] points = Enumerable
            .Range(0, pointsCount)
            .Select(index => new MarketDataPointDto(
                AssetId: Guid.NewGuid(),
                MarketDataSourceId: Guid.NewGuid(),
                TimestampUtc: FromUtc.AddDays(index),
                PriceUsd: 100m + index,
                MarketCapUsd: null,
                Volume24hUsd: 1000m + index))
            .ToArray();

        Mock<IGetHistoricalMarketDataUseCase> useCase = new();

        useCase
            .Setup(x => x.ExecuteAsync(
                It.IsAny<HistoricalMarketDataRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoricalMarketDataResponse(
                Symbol: "BTC",
                MarketDataSourceCode: "BINANCE",
                Points: points));

        return useCase;
    }

    private static Mock<IAnalyzePriceChangeUseCase> CreatePriceChangeUseCase(
        int pointsCount = 2)
    {
        Mock<IAnalyzePriceChangeUseCase> useCase = new();

        useCase
            .Setup(x => x.ExecuteAsync(
                It.IsAny<PriceChangeAnalysisRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceChangeAnalysisResponse(
                Symbol: "BTC",
                MarketDataSourceCode: "BINANCE",
                FromUtc: FromUtc,
                ToUtc: ToUtc,
                StartPriceUsd: pointsCount < 2 ? null : 100m,
                EndPriceUsd: pointsCount < 2 ? null : 110m,
                AbsoluteChangeUsd: pointsCount < 2 ? null : 10m,
                PercentageChange: pointsCount < 2 ? null : 10m,
                PointsCount: pointsCount));

        return useCase;
    }

    private static Mock<ICalculateVolatilityUseCase> CreateVolatilityUseCase(
        int pointsCount = 2,
        int returnsCount = 1)
    {
        Mock<ICalculateVolatilityUseCase> useCase = new();

        useCase
            .Setup(x => x.ExecuteAsync(
                It.IsAny<VolatilityAnalysisRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VolatilityAnalysisResponse(
                Symbol: "BTC",
                MarketDataSourceCode: "BINANCE",
                FromUtc: FromUtc,
                ToUtc: ToUtc,
                PointsCount: pointsCount,
                ReturnsCount: returnsCount,
                AverageReturnPercent: returnsCount < 2 ? null : 1m,
                VolatilityPercent: returnsCount < 2 ? null : 2m));

        return useCase;
    }

    private static Mock<ICalculateCorrelationUseCase> CreateCorrelationUseCase()
    {
        Mock<ICalculateCorrelationUseCase> useCase = new();

        useCase
            .Setup(x => x.ExecuteAsync(
                It.IsAny<CorrelationAnalysisRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CorrelationAnalysisResponse(
                BaseSymbol: "BTC",
                QuoteSymbol: "ETH",
                MarketDataSourceCode: "BINANCE",
                FromUtc: FromUtc,
                ToUtc: ToUtc,
                BasePointsCount: 2,
                QuotePointsCount: 2,
                MatchedReturnsCount: 1,
                PearsonCorrelation: null));

        return useCase;
    }
}