#pragma warning disable

using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Application.Contracts.Reports;
using CryptoMarketAnalysis.Infrastructure.Reports;
using FluentAssertions;
using System.Text;

namespace CryptoMarketAnalysis.Infrastructure.Tests.Reports;

public sealed class PdfReportGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldReturnValidPdfBytes()
    {
        // Arrange
        var generator = new PdfReportGenerator();

        DateTime fromUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        var report = new MarketAnalysisReportModel(
            Symbol: "BTC",
            MarketDataSourceCode: "BINANCE",
            FromUtc: fromUtc,
            ToUtc: toUtc,
            GeneratedAtUtc: DateTime.UtcNow,
            Points:
            new List<MarketAnalysisReportPointDto>
            {
                new MarketAnalysisReportPointDto(
                    fromUtc,
                    71408.90m,
                    null,
                    1723958338.68m),

                new MarketAnalysisReportPointDto(
                    fromUtc.AddDays(1),
                    66760.83m,
                    null,
                    2253348577.98m),
            }.AsReadOnly(),
            PriceChange: new PriceChangeAnalysisResponse(
                Symbol: "BTC",
                MarketDataSourceCode: "BINANCE",
                FromUtc: fromUtc,
                ToUtc: toUtc,
                StartPriceUsd: 71408.90m,
                EndPriceUsd: 66760.83m,
                AbsoluteChangeUsd: -4648.07m,
                PercentageChange: -6.509m,
                PointsCount: 2),
            Volatility: new VolatilityAnalysisResponse(
                Symbol: "BTC",
                MarketDataSourceCode: "BINANCE",
                FromUtc: fromUtc,
                ToUtc: toUtc,
                PointsCount: 2,
                ReturnsCount: 1,
                AverageReturnPercent: null,
                VolatilityPercent: null),
            Correlation: null);

        // Act
        byte[] bytes = await generator.GenerateAsync(report);

        // Assert
        bytes.Should().NotBeEmpty();

        string pdfHeader = Encoding.ASCII.GetString(bytes.Take(4).ToArray());
        pdfHeader.Should().Be("%PDF");
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnValidPdfBytes_WhenCorrelationIsProvided()
    {
        // Arrange
        var generator = new PdfReportGenerator();

        DateTime fromUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        var report = new MarketAnalysisReportModel(
            Symbol: "BTC",
            MarketDataSourceCode: "BINANCE",
            FromUtc: fromUtc,
            ToUtc: toUtc,
            GeneratedAtUtc: DateTime.UtcNow,
            Points:
            new List<MarketAnalysisReportPointDto>
            {
                new MarketAnalysisReportPointDto(
                    fromUtc,
                    71408.90m,
                    null,
                    1723958338.68m),

                new MarketAnalysisReportPointDto(
                    fromUtc.AddDays(1),
                    66760.83m,
                    null,
                    2253348577.98m),
            }.AsReadOnly(),
            PriceChange: new PriceChangeAnalysisResponse(
                Symbol: "BTC",
                MarketDataSourceCode: "BINANCE",
                FromUtc: fromUtc,
                ToUtc: toUtc,
                StartPriceUsd: 71408.90m,
                EndPriceUsd: 66760.83m,
                AbsoluteChangeUsd: -4648.07m,
                PercentageChange: -6.509m,
                PointsCount: 2),
            Volatility: new VolatilityAnalysisResponse(
                Symbol: "BTC",
                MarketDataSourceCode: "BINANCE",
                FromUtc: fromUtc,
                ToUtc: toUtc,
                PointsCount: 2,
                ReturnsCount: 1,
                AverageReturnPercent: null,
                VolatilityPercent: null),
            Correlation: new CorrelationAnalysisResponse(
                BaseSymbol: "BTC",
                QuoteSymbol: "ETH",
                MarketDataSourceCode: "BINANCE",
                FromUtc: fromUtc,
                ToUtc: toUtc,
                BasePointsCount: 10,
                QuotePointsCount: 10,
                MatchedReturnsCount: 9,
                PearsonCorrelation: 0.8984m));

        // Act
        byte[] bytes = await generator.GenerateAsync(report);

        // Assert
        bytes.Should().NotBeEmpty();

        string pdfHeader = Encoding.ASCII.GetString(bytes.Take(4).ToArray());
        pdfHeader.Should().Be("%PDF");
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrow_WhenReportIsNull()
    {
        // Arrange
        var generator = new PdfReportGenerator();

        // Act
        Func<Task> act = async () => await generator.GenerateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateAsync_ShouldNotSuppressOperationCanceledException()
    {
        // Arrange
        var generator = new PdfReportGenerator();

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act
        Func<Task> act = async () => await generator.GenerateAsync(
            CreateValidReport(),
            cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static MarketAnalysisReportModel CreateValidReport()
    {
        DateTime fromUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        return new MarketAnalysisReportModel(
            Symbol: "BTC",
            MarketDataSourceCode: "BINANCE",
            FromUtc: fromUtc,
            ToUtc: toUtc,
            GeneratedAtUtc: DateTime.UtcNow,
            Points:
            new List<MarketAnalysisReportPointDto>
            {
                new MarketAnalysisReportPointDto(
                    fromUtc,
                    71408.90m,
                    null,
                    1723958338.68m),
            }.AsReadOnly(),
            PriceChange: new PriceChangeAnalysisResponse(
                "BTC",
                "BINANCE",
                fromUtc,
                toUtc,
                71408.90m,
                71408.90m,
                0m,
                0m,
                1),
            Volatility: new VolatilityAnalysisResponse(
                "BTC",
                "BINANCE",
                fromUtc,
                toUtc,
                1,
                0,
                null,
                null),
            Correlation: null);
    }
}