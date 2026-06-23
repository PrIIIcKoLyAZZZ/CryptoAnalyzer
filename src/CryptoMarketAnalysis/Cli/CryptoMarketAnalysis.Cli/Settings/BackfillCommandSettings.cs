using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CryptoMarketAnalysis.Cli.Settings;

public sealed class BackfillCommandSettings : CommandSettings
{
    [CommandOption("--symbols")]
    [Description("Asset symbols. Repeat option for multiple symbols.")]
    public string[] Symbols { get; init; } = [];

    [CommandOption("--from")]
    [Description("Start date in UTC.")]
    public DateTime FromUtc { get; init; }

    [CommandOption("--to")]
    [Description("End date in UTC.")]
    public DateTime ToUtc { get; init; }

    [CommandOption("--sources")]
    [Description("Market data source codes. Repeat option for multiple sources.")]
    public string[] Sources { get; init; } = [];

    [CommandOption("--batch-days")]
    [Description("Batch size in days.")]
    public int BatchDays { get; init; } = 30;

    public override ValidationResult Validate()
    {
        if (Symbols.Length == 0 || Symbols.Any(string.IsNullOrWhiteSpace))
            return ValidationResult.Error("At least one --symbols value is required.");

        if (Sources.Length == 0 || Sources.Any(string.IsNullOrWhiteSpace))
            return ValidationResult.Error("At least one --sources value is required.");

        if (FromUtc == default)
            return ValidationResult.Error("--from is required.");

        if (ToUtc == default)
            return ValidationResult.Error("--to is required.");

        if (FromUtc > ToUtc)
            return ValidationResult.Error("--from must be less than or equal to --to.");

        if (BatchDays <= 0)
            return ValidationResult.Error("--batch-days must be greater than zero.");

        return ValidationResult.Success();
    }
}