using CryptoMarketAnalysis.Application.Contracts.MarketData.External;
using CryptoMarketAnalysis.Infrastructure.MarketData.CoinGecko;
using FluentAssertions;

namespace CryptoMarketAnalysis.Infrastructure.Tests.MarketData.CoinGecko;

public sealed class CoinGeckoMarketDataMapperTests
{
    [Fact]
    public void ParseHistoricalResponse_ShouldMapPricesMarketCapsAndVolumesByTimestamp()
    {
        // Arrange
        const string json = """
        {
          "prices": [[1780272000000, 70500.50]],
          "market_caps": [[1780272000000, 1400000000000.25]],
          "total_volumes": [[1780272000000, 9876543.21]]
        }
        """;

        // Act
        List<ExternalMarketDataPointDto> points = CoinGeckoMarketDataMapper.ParseHistoricalResponse(json);

        // Assert
        points.Should().ContainSingle();

        ExternalMarketDataPointDto point = points.Single();
        point.TimestampUtc.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        point.PriceUsd.Should().Be(70500.50m);
        point.MarketCapUsd.Should().Be(1400000000000.25m);
        point.Volume24hUsd.Should().Be(9876543.21m);
    }

    [Fact]
    public void ParseHistoricalResponse_ShouldSetMarketCapToNull_WhenTimestampIsMissingInMarketCaps()
    {
        // Arrange
        const string json = """
        {
          "prices": [[1780272000000, 70500.50]],
          "market_caps": [],
          "total_volumes": [[1780272000000, 9876543.21]]
        }
        """;

        // Act
        List<ExternalMarketDataPointDto> points = CoinGeckoMarketDataMapper.ParseHistoricalResponse(json);

        // Assert
        points.Single().MarketCapUsd.Should().BeNull();
        points.Single().Volume24hUsd.Should().Be(9876543.21m);
    }

    [Fact]
    public void ParseHistoricalResponse_ShouldSetVolumeToNull_WhenTimestampIsMissingInVolumes()
    {
        // Arrange
        const string json = """
        {
          "prices": [[1780272000000, 70500.50]],
          "market_caps": [[1780272000000, 1400000000000.25]],
          "total_volumes": []
        }
        """;

        // Act
        List<ExternalMarketDataPointDto> points = CoinGeckoMarketDataMapper.ParseHistoricalResponse(json);

        // Assert
        points.Single().MarketCapUsd.Should().Be(1400000000000.25m);
        points.Single().Volume24hUsd.Should().BeNull();
    }

    [Fact]
    public void ParseHistoricalResponse_ShouldReturnPointsSortedByTimestamp()
    {
        // Arrange
        const string json = """
        {
          "prices": [
            [1780444800000, 72000.00],
            [1780272000000, 70500.50],
            [1780358400000, 71000.00]
          ],
          "market_caps": [],
          "total_volumes": []
        }
        """;

        // Act
        List<ExternalMarketDataPointDto> points = CoinGeckoMarketDataMapper.ParseHistoricalResponse(json);

        // Assert
        points.Select(point => point.TimestampUtc)
            .Should()
            .BeInAscendingOrder();
    }

    [Fact]
    public void ParseHistoricalResponse_ShouldThrow_WhenJsonIsInvalid()
    {
        // Arrange
        const string json = "{ invalid json";

        // Act
        Action act = () => CoinGeckoMarketDataMapper.ParseHistoricalResponse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("CoinGecko returned invalid historical JSON response.");
    }

    [Fact]
    public void ParseHistoricalResponse_ShouldThrow_WhenPricesFieldHasInvalidFormat()
    {
        // Arrange
        const string json = """
        {
          "prices": {},
          "market_caps": [],
          "total_volumes": []
        }
        """;

        // Act
        Action act = () => CoinGeckoMarketDataMapper.ParseHistoricalResponse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("CoinGecko field 'prices' has invalid format.");
    }

    [Fact]
    public void ParseLatestResponse_ShouldMapLatestPoint()
    {
        // Arrange
        const string json = """
        {
          "bitcoin": {
            "usd": 70500.50,
            "usd_market_cap": 1400000000000.25,
            "usd_24h_vol": 9876543.21,
            "last_updated_at": 1780272000
          }
        }
        """;

        // Act
        ExternalMarketDataPointDto? point = CoinGeckoMarketDataMapper.ParseLatestResponse(json, "bitcoin", "usd");

        // Assert
        point.Should().NotBeNull();
        point?.TimestampUtc.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        point?.PriceUsd.Should().Be(70500.50m);
        point?.MarketCapUsd.Should().Be(1400000000000.25m);
        point?.Volume24hUsd.Should().Be(9876543.21m);
    }

    [Fact]
    public void ParseLatestResponse_ShouldReturnNull_WhenCoinIdIsMissing()
    {
        // Arrange
        const string json = """
        {
          "ethereum": {
            "usd": 3500.00,
            "last_updated_at": 1780272000
          }
        }
        """;

        // Act
        ExternalMarketDataPointDto? point = CoinGeckoMarketDataMapper.ParseLatestResponse(json, "bitcoin", "usd");

        // Assert
        point.Should().BeNull();
    }

    [Fact]
    public void ParseLatestResponse_ShouldReturnNull_WhenPriceIsMissing()
    {
        // Arrange
        const string json = """
        {
          "bitcoin": {
            "usd_market_cap": 1400000000000.25,
            "usd_24h_vol": 9876543.21,
            "last_updated_at": 1780272000
          }
        }
        """;

        // Act
        ExternalMarketDataPointDto? point = CoinGeckoMarketDataMapper.ParseLatestResponse(json, "bitcoin", "usd");

        // Assert
        point.Should().BeNull();
    }

    [Fact]
    public void ParseLatestResponse_ShouldThrow_WhenLastUpdatedAtIsMissing()
    {
        // Arrange
        const string json = """
        {
          "bitcoin": {
            "usd": 70500.50
          }
        }
        """;

        // Act
        Action act = () => CoinGeckoMarketDataMapper.ParseLatestResponse(json, "bitcoin", "usd");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("CoinGecko latest response does not contain last_updated_at.");
    }

    [Fact]
    public void ParseLatestResponse_ShouldThrow_WhenJsonIsInvalid()
    {
        // Arrange
        const string json = "{ invalid json";

        // Act
        Action act = () => CoinGeckoMarketDataMapper.ParseLatestResponse(json, "bitcoin", "usd");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("CoinGecko returned invalid latest JSON response.");
    }
}