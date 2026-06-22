using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CryptoMarketAnalysis.Cli.Settings;

public sealed class LoadCommandSettings : CommandSettings
{
    [CommandOption("--symbols")]
    [Description("Asset symbols to load, for example: BTC ETH.")]
    public string[] Symbols { get; init; } = [];

    [CommandOption("--from")]
    [Description("Start date in UTC, for example: 2026-06-01.")]
    public DateTime FromUtc { get; init; }

    [CommandOption("--to")]
    [Description("End date in UTC, for example: 2026-06-10.")]
    public DateTime ToUtc { get; init; }

    [CommandOption("--source")]
    [Description("Market data source code, for example: BINANCE or COINGECKO. Optional.")]
    public string? MarketDataSourceCode { get; init; }

    public override ValidationResult Validate()
    {
        if (Symbols.Length == 0)
        {
            return ValidationResult.Error("At least one symbol must be provided.");
        }

        if (Symbols.Any(string.IsNullOrWhiteSpace))
        {
            return ValidationResult.Error("Symbols cannot contain empty values.");
        }

        if (FromUtc == default)
        {
            return ValidationResult.Error("--from is required.");
        }

        if (ToUtc == default)
        {
            return ValidationResult.Error("--to is required.");
        }

        if (FromUtc >= ToUtc)
        {
            return ValidationResult.Error("--from must be earlier than --to.");
        }

        return ValidationResult.Success();
    }
}