using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;

namespace CryptoMarketAnalysis.Cli.Commands.Analytics;

public sealed class VolatilityCommand : AsyncCommand<VolatilityCommandSettings>
{
    private readonly ICalculateVolatilityUseCase _useCase;

    public VolatilityCommand(
        ICalculateVolatilityUseCase useCase)
    {
        _useCase = useCase;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        VolatilityCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var fromUtc = DateTime.SpecifyKind(settings.FromUtc.Date, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(settings.ToUtc.Date, DateTimeKind.Utc);

        VolatilityAnalysisResponse response = await _useCase.ExecuteAsync(
            new VolatilityAnalysisRequest(
                Symbol: settings.Symbol,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                MarketDataSourceCode: settings.MarketDataSourceCode),
            cancellationToken);

        WriteResponse(response);

        return 0;
    }

    private static void WriteResponse(
        VolatilityAnalysisResponse response)
    {
        Table table = new Table()
            .Title("Volatility analysis")
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Value");

        table.AddRow("Symbol", response.Symbol);
        table.AddRow("Source", response.MarketDataSourceCode ?? "ALL");
        table.AddRow("From UTC", FormatDateTime(response.FromUtc));
        table.AddRow("To UTC", FormatDateTime(response.ToUtc));
        table.AddRow("Points count", response.PointsCount.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Returns count", response.ReturnsCount.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Average return", FormatPercent(response.AverageReturnPercent));
        table.AddRow("Volatility", FormatPercent(response.VolatilityPercent));

        AnsiConsole.Write(table);
    }

    private static string FormatDateTime(
        DateTime value)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static string FormatPercent(
        decimal? value)
    {
        return value.HasValue
            ? $"{value.Value.ToString("N4", CultureInfo.InvariantCulture)}%"
            : "N/A";
    }
}