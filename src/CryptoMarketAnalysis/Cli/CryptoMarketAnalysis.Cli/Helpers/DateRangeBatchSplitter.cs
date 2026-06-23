namespace CryptoMarketAnalysis.Cli.Helpers;

public static class DateRangeBatchSplitter
{
    public static IReadOnlyCollection<DateRangeBatch> Split(
        DateTime fromUtc,
        DateTime toUtc,
        int batchDays)
    {
        if (batchDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchDays), "Batch days must be greater than zero.");

        DateTime normalizedFromUtc = NormalizeDate(fromUtc);
        DateTime normalizedToUtc = NormalizeDate(toUtc);

        if (normalizedFromUtc > normalizedToUtc)
            throw new ArgumentException("From date must be less than or equal to To date.");

        var batches = new List<DateRangeBatch>();

        DateTime currentFromUtc = normalizedFromUtc;

        while (currentFromUtc <= normalizedToUtc)
        {
            DateTime currentToUtc = currentFromUtc
                .AddDays(batchDays - 1);

            if (currentToUtc > normalizedToUtc)
                currentToUtc = normalizedToUtc;

            batches.Add(new DateRangeBatch(
                currentFromUtc,
                currentToUtc));

            currentFromUtc = currentToUtc.AddDays(1);
        }

        return batches;
    }

    private static DateTime NormalizeDate(
        DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }
}