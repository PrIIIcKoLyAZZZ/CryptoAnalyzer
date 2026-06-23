namespace CryptoMarketAnalysis.Worker.Options;

public sealed class MarketDataLoadOptions
{
    public static string SectionName { get; set; } = "Worker:MarketDataLoad";

    public bool Enabled { get; init; }

    public int IntervalMinutes { get; init; } = 1440;

    public int DaysBack { get; init; } = 1;

    public string[] Symbols { get; init; } = [];

    public string? MarketDataSourceCode { get; init; }
}