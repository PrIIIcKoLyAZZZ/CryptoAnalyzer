using CryptoMarketAnalysis.Application.Contracts.MarketData.External;
using CryptoMarketAnalysis.Infrastructure.MarketData.Binance;
using FluentAssertions;

namespace CryptoMarketAnalysis.Infrastructure.Tests.MarketData.Binance;

public sealed class BinanceMarketDataMapperTests
{
    [Fact]
    public void ParseKlines_ShouldMapHistoricalKline()
    {
        // Arrange
        const string json = """
        [
          [
            1780272000000,
            "70000.00",
            "71000.00",
            "69000.00",
            "70500.50",
            "123.45",
            1780358399999,
            "9876543.21"
          ]
        ]
        """;

        // Act
        List<ExternalMarketDataPointDto> points = BinanceMarketDataMapper.ParseKlines(json);

        // Assert
        points.Should().ContainSingle();

        ExternalMarketDataPointDto point = points.Single();
        point.TimestampUtc.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        point.PriceUsd.Should().Be(70500.50m);
        point.MarketCapUsd.Should().BeNull();
        point.Volume24hUsd.Should().Be(9876543.21m);
    }

    [Fact]
    public void ParseKlines_ShouldThrow_WhenResponseIsNotArray()
    {
        // Arrange
        const string json = "{}";

        // Act
        Action act = () => BinanceMarketDataMapper.ParseKlines(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Binance klines response has invalid format.");
    }

    [Fact]
    public void ParseKlines_ShouldThrow_WhenKlineItemHasInvalidFormat()
    {
        // Arrange
        const string json = """
        [
          [1780272000000, "70000.00"]
        ]
        """;

        // Act
        Action act = () => BinanceMarketDataMapper.ParseKlines(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Binance kline item has invalid format.");
    }

    [Fact]
    public void ParseKlines_ShouldThrow_WhenJsonIsInvalid()
    {
        // Arrange
        const string json = "{ invalid json";

        // Act
        Action act = () => BinanceMarketDataMapper.ParseKlines(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Binance klines response contains invalid JSON.");
    }

    [Fact]
    public void ParseKlines_ShouldThrow_WhenClosePriceIsNotPositive()
    {
        // Arrange
        const string json = """
        [
          [
            1780272000000,
            "70000.00",
            "71000.00",
            "69000.00",
            "0",
            "123.45",
            1780358399999,
            "9876543.21"
          ]
        ]
        """;

        // Act
        Action act = () => BinanceMarketDataMapper.ParseKlines(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Binance close price must be greater than zero.");
    }

    [Fact]
    public void ParseKlines_ShouldThrow_WhenQuoteVolumeIsNegative()
    {
        // Arrange
        const string json = """
        [
          [
            1780272000000,
            "70000.00",
            "71000.00",
            "69000.00",
            "70500.50",
            "123.45",
            1780358399999,
            "-1"
          ]
        ]
        """;

        // Act
        Action act = () => BinanceMarketDataMapper.ParseKlines(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Binance quote volume must not be negative.");
    }

    [Fact]
    public void ParseLatest_ShouldMapTicker24Hr()
    {
        // Arrange
        const string json = """
        {
          "symbol": "BTCUSDT",
          "lastPrice": "70500.50",
          "quoteVolume": "9876543.21",
          "closeTime": 1780272000000
        }
        """;

        // Act
        ExternalMarketDataPointDto point = BinanceMarketDataMapper.ParseLatest(json);

        // Assert
        point.TimestampUtc.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        point.PriceUsd.Should().Be(70500.50m);
        point.MarketCapUsd.Should().BeNull();
        point.Volume24hUsd.Should().Be(9876543.21m);
    }

    [Fact]
    public void ParseLatest_ShouldThrow_WhenJsonIsInvalid()
    {
        // Arrange
        const string json = "{ invalid json";

        // Act
        Action act = () => BinanceMarketDataMapper.ParseLatest(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Binance ticker response contains invalid JSON.");
    }

    [Fact]
    public void ParseLatest_ShouldThrow_WhenRequiredFieldIsMissing()
    {
        // Arrange
        const string json = """
        {
          "symbol": "BTCUSDT",
          "lastPrice": "70500.50",
          "closeTime": 1780272000000
        }
        """;

        // Act
        Action act = () => BinanceMarketDataMapper.ParseLatest(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Binance ticker response is missing required fields.");
    }

    [Fact]
    public void ParseLatest_ShouldThrow_WhenLastPriceIsNotPositive()
    {
        // Arrange
        const string json = """
        {
          "symbol": "BTCUSDT",
          "lastPrice": "0",
          "quoteVolume": "9876543.21",
          "closeTime": 1780272000000
        }
        """;

        // Act
        Action act = () => BinanceMarketDataMapper.ParseLatest(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Binance lastPrice must be greater than zero.");
    }

    [Fact]
    public void ParseLatest_ShouldThrow_WhenQuoteVolumeIsNegative()
    {
        // Arrange
        const string json = """
        {
          "symbol": "BTCUSDT",
          "lastPrice": "70500.50",
          "quoteVolume": "-1",
          "closeTime": 1780272000000
        }
        """;

        // Act
        Action act = () => BinanceMarketDataMapper.ParseLatest(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Binance quoteVolume must not be negative.");
    }
}