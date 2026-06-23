namespace CryptoMarketAnalysis.Cli.Helpers;

public sealed record DateRangeBatch(
    DateTime FromUtc,
    DateTime ToUtc);