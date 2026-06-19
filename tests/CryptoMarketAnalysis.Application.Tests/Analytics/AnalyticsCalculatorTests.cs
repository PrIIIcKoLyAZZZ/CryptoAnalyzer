using CryptoMarketAnalysis.Application.Analytics;
using FluentAssertions;

namespace CryptoMarketAnalysis.Application.Tests.Analytics;

public sealed class AnalyticsCalculatorTests
{
    [Fact]
    public void CalculateReturns_ShouldCalculatePercentReturns()
    {
        // Arrange
        decimal[] prices = [100m, 110m, 99m];

        // Act
        IReadOnlyCollection<decimal> returns = AnalyticsCalculator.CalculateReturns(prices);

        // Assert
        returns.Should().Equal(10m, -10m);
    }

    [Fact]
    public void CalculateReturns_ShouldReturnEmpty_WhenPricesCountIsLessThanTwo()
    {
        // Arrange
        decimal[] prices = [100m];

        // Act
        IReadOnlyCollection<decimal> returns = AnalyticsCalculator.CalculateReturns(prices);

        // Assert
        returns.Should().BeEmpty();
    }

    [Fact]
    public void CalculateAverage_ShouldReturnAverage()
    {
        // Arrange
        decimal[] values = [1m, 2m, 3m];

        // Act
        decimal? average = AnalyticsCalculator.CalculateAverage(values);

        // Assert
        average.Should().Be(2m);
    }

    [Fact]
    public void CalculateAverage_ShouldReturnNull_WhenValuesAreEmpty()
    {
        // Arrange
        decimal[] values = [];

        // Act
        decimal? average = AnalyticsCalculator.CalculateAverage(values);

        // Assert
        average.Should().BeNull();
    }

    [Fact]
    public void CalculateSampleStandardDeviation_ShouldReturnSampleStandardDeviation()
    {
        // Arrange
        decimal[] values = [2m, 4m, 4m, 4m, 5m, 5m, 7m, 9m];

        // Act
        decimal? standardDeviation = AnalyticsCalculator.CalculateSampleStandardDeviation(values);

        // Assert
        standardDeviation.Should().NotBeNull();
        standardDeviation.Value.Should().BeApproximately(2.138089935m, 0.000001m);
    }

    [Fact]
    public void CalculateSampleStandardDeviation_ShouldReturnNull_WhenValuesCountIsLessThanTwo()
    {
        // Arrange
        decimal[] values = [1m];

        // Act
        decimal? standardDeviation = AnalyticsCalculator.CalculateSampleStandardDeviation(values);

        // Assert
        standardDeviation.Should().BeNull();
    }

    [Fact]
    public void CalculateSampleStandardDeviation_ShouldReturnZero_WhenValuesAreConstant()
    {
        // Arrange
        decimal[] values = [5m, 5m, 5m];

        // Act
        decimal? standardDeviation = AnalyticsCalculator.CalculateSampleStandardDeviation(values);

        // Assert
        standardDeviation.Should().Be(0m);
    }

    [Fact]
    public void CalculatePearsonCorrelation_ShouldReturnOne_ForPerfectPositiveCorrelation()
    {
        // Arrange
        decimal[] x = [1m, 2m, 3m];
        decimal[] y = [2m, 4m, 6m];

        // Act
        decimal? correlation = AnalyticsCalculator.CalculatePearsonCorrelation(x, y);

        // Assert
        correlation.Should().NotBeNull();
        correlation.Value.Should().BeApproximately(1m, 0.000001m);
    }

    [Fact]
    public void CalculatePearsonCorrelation_ShouldReturnMinusOne_ForPerfectNegativeCorrelation()
    {
        // Arrange
        decimal[] x = [1m, 2m, 3m];
        decimal[] y = [6m, 4m, 2m];

        // Act
        decimal? correlation = AnalyticsCalculator.CalculatePearsonCorrelation(x, y);

        // Assert
        correlation.Should().NotBeNull();
        correlation.Value.Should().BeApproximately(-1m, 0.000001m);
    }

    [Fact]
    public void CalculatePearsonCorrelation_ShouldReturnNull_WhenSeriesHaveLessThanTwoValues()
    {
        // Arrange
        decimal[] x = [1m];
        decimal[] y = [2m];

        // Act
        decimal? correlation = AnalyticsCalculator.CalculatePearsonCorrelation(x, y);

        // Assert
        correlation.Should().BeNull();
    }

    [Fact]
    public void CalculatePearsonCorrelation_ShouldReturnNull_WhenOneSeriesHasZeroVariance()
    {
        // Arrange
        decimal[] x = [1m, 1m, 1m];
        decimal[] y = [1m, 2m, 3m];

        // Act
        decimal? correlation = AnalyticsCalculator.CalculatePearsonCorrelation(x, y);

        // Assert
        correlation.Should().BeNull();
    }

    [Fact]
    public void CalculatePearsonCorrelation_ShouldThrow_WhenSeriesHaveDifferentLength()
    {
        // Arrange
        decimal[] x = [1m, 2m];
        decimal[] y = [1m, 2m, 3m];

        // Act
        Action act = () => AnalyticsCalculator.CalculatePearsonCorrelation(x, y);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Series must have the same length.");
    }
}