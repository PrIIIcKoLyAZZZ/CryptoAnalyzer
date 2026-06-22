using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CryptoMarketAnalysis.Cli.Settings;

public sealed class CorrelationCommandSettings : CommandSettings
{
    [CommandOption("--base")]
    [Description("Base asset symbol, for example: BTC.")]
    public string BaseSymbol { get; init; } = string.Empty;

    [CommandOption("--quote")]
    [Description("Quote asset symbol, for example: ETH.")]
    public string QuoteSymbol { get; init; } = string.Empty;

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
        if (string.IsNullOrWhiteSpace(BaseSymbol))
            return ValidationResult.Error("--base is required.");

        if (string.IsNullOrWhiteSpace(QuoteSymbol))
            return ValidationResult.Error("--quote is required.");

        if (FromUtc == default)
            return ValidationResult.Error("--from is required.");

        if (ToUtc == default)
            return ValidationResult.Error("--to is required.");

        if (FromUtc >= ToUtc)
            return ValidationResult.Error("--from must be earlier than --to.");

        return ValidationResult.Success();
    }
}