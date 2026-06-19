namespace CryptoMarketAnalysis.Application.Analytics;

internal static class AnalyticsCalculator
{
    public static IReadOnlyCollection<decimal> CalculateReturns(
        IReadOnlyCollection<decimal> prices)
    {
        if (prices.Count < 2)
            return Array.Empty<decimal>();

        decimal[] values = prices.ToArray();
        var returns = new List<decimal>(values.Length - 1);

        for (int i = 1; i < values.Length; i++)
        {
            decimal previous = values[i - 1];
            decimal current = values[i];

            if (previous == 0)
                continue;

            decimal returnPercent = (current - previous) / previous * 100m;
            returns.Add(returnPercent);
        }

        return returns;
    }

    public static decimal? CalculateAverage(
        IReadOnlyCollection<decimal> values)
    {
        if (values.Count == 0)
            return null;

        return values.Average();
    }

    public static decimal? CalculateSampleStandardDeviation(
        IReadOnlyCollection<decimal> values)
    {
        if (values.Count < 2)
            return null;

        decimal average = values.Average();
        decimal variance = values.Sum(value => (value - average) * (value - average)) / (values.Count - 1);

        return Sqrt(variance);
    }

    public static decimal? CalculatePearsonCorrelation(
        IReadOnlyCollection<decimal> x,
        IReadOnlyCollection<decimal> y)
    {
        if (x.Count != y.Count)
            throw new ArgumentException("Series must have the same length.");

        if (x.Count < 2)
            return null;

        decimal[] xValues = x.ToArray();
        decimal[] yValues = y.ToArray();

        decimal xAverage = xValues.Average();
        decimal yAverage = yValues.Average();

        decimal covarianceSum = 0m;
        decimal xVarianceSum = 0m;
        decimal yVarianceSum = 0m;

        for (int i = 0; i < xValues.Length; i++)
        {
            decimal xDiff = xValues[i] - xAverage;
            decimal yDiff = yValues[i] - yAverage;

            covarianceSum += xDiff * yDiff;
            xVarianceSum += xDiff * xDiff;
            yVarianceSum += yDiff * yDiff;
        }

        if (xVarianceSum == 0m || yVarianceSum == 0m)
            return null;

        return covarianceSum / (Sqrt(xVarianceSum) * Sqrt(yVarianceSum));
    }

    private static decimal Sqrt(decimal value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be negative.");

        if (value == 0)
            return 0;

        return (decimal)Math.Sqrt((double)value);
    }
}