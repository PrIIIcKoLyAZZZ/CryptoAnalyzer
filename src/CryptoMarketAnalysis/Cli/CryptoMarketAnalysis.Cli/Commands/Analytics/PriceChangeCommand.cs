using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;

namespace CryptoMarketAnalysis.Cli.Commands.Analytics;

public sealed class PriceChangeCommand : AsyncCommand<PriceChangeCommandSettings>
{
    private readonly IAnalyzePriceChangeUseCase _useCase;

    public PriceChangeCommand(
        IAnalyzePriceChangeUseCase useCase)
    {
        _useCase = useCase;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        PriceChangeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var fromUtc = DateTime.SpecifyKind(settings.FromUtc.Date, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(settings.ToUtc.Date, DateTimeKind.Utc);

        PriceChangeAnalysisResponse response = await _useCase.ExecuteAsync(
            new PriceChangeAnalysisRequest(
                Symbol: settings.Symbol,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                MarketDataSourceCode: settings.MarketDataSourceCode),
            cancellationToken);

        WriteResponse(response);

        return 0;
    }

    private static void WriteResponse(
        PriceChangeAnalysisResponse response)
    {
        Table table = new Table()
            .Title("Price change analysis")
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Value");

        table.AddRow("Symbol", response.Symbol);
        table.AddRow("Source", response.MarketDataSourceCode ?? "ALL");
        table.AddRow("From UTC", FormatDateTime(response.FromUtc));
        table.AddRow("To UTC", FormatDateTime(response.ToUtc));
        table.AddRow("Points count", response.PointsCount.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Start price USD", FormatNullableDecimal(response.StartPriceUsd));
        table.AddRow("End price USD", FormatNullableDecimal(response.EndPriceUsd));
        table.AddRow("Absolute change USD", FormatNullableDecimal(response.AbsoluteChangeUsd));
        table.AddRow("Percentage change", FormatPercent(response.PercentageChange));

        AnsiConsole.Write(table);
    }

    private static string FormatDateTime(
        DateTime value)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static string FormatNullableDecimal(
        decimal? value)
    {
        return value.HasValue
            ? value.Value.ToString("N4", CultureInfo.InvariantCulture)
            : "N/A";
    }

    private static string FormatPercent(
        decimal? value)
    {
        return value.HasValue
            ? $"{value.Value.ToString("N4", CultureInfo.InvariantCulture)}%"
            : "N/A";
    }
}