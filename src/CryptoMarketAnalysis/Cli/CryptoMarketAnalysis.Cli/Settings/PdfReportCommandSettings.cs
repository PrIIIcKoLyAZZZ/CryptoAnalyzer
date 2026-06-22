using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CryptoMarketAnalysis.Cli.Settings;

public sealed class PdfReportCommandSettings : CommandSettings
{
    [CommandOption("--symbol")]
    [Description("Asset symbol, for example: BTC.")]
    public string Symbol { get; init; } = string.Empty;

    [CommandOption("--from")]
    [Description("Start date in UTC, for example: 2026-06-01.")]
    public DateTime FromUtc { get; init; }

    [CommandOption("--to")]
    [Description("End date in UTC, for example: 2026-06-10.")]
    public DateTime ToUtc { get; init; }

    [CommandOption("--source")]
    [Description("Market data source code, for example: BINANCE or COINGECKO. Optional.")]
    public string? MarketDataSourceCode { get; init; }

    [CommandOption("--correlation")]
    [Description("Correlation symbol, for example: ETH. Optional.")]
    public string? CorrelationSymbol { get; init; }

    [CommandOption("--output")]
    [Description("Output PDF path. Optional.")]
    public string? OutputPath { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
            return ValidationResult.Error("--symbol is required.");

        if (FromUtc == default)
            return ValidationResult.Error("--from is required.");

        if (ToUtc == default)
            return ValidationResult.Error("--to is required.");

        if (FromUtc >= ToUtc)
            return ValidationResult.Error("--from must be earlier than --to.");

        return ValidationResult.Success();
    }
}