using CryptoMarketAnalysis.Application.Contracts.Analytics;
using CryptoMarketAnalysis.Cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;

namespace CryptoMarketAnalysis.Cli.Commands.Analytics;

public sealed class CorrelationCommand : AsyncCommand<CorrelationCommandSettings>
{
    private readonly ICalculateCorrelationUseCase _useCase;

    public CorrelationCommand(
        ICalculateCorrelationUseCase useCase)
    {
        _useCase = useCase;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CorrelationCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var fromUtc = DateTime.SpecifyKind(settings.FromUtc.Date, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(settings.ToUtc.Date, DateTimeKind.Utc);

        CorrelationAnalysisResponse response = await _useCase.ExecuteAsync(
            new CorrelationAnalysisRequest(
                BaseSymbol: settings.BaseSymbol,
                QuoteSymbol: settings.QuoteSymbol,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                MarketDataSourceCode: settings.MarketDataSourceCode),
            cancellationToken);

        WriteResponse(response);

        return 0;
    }

    private static void WriteResponse(
        CorrelationAnalysisResponse response)
    {
        Table table = new Table()
            .Title("Correlation analysis")
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Value");

        table.AddRow("Base symbol", response.BaseSymbol);
        table.AddRow("Quote symbol", response.QuoteSymbol);
        table.AddRow("Source", response.MarketDataSourceCode ?? "ALL");
        table.AddRow("From UTC", FormatDateTime(response.FromUtc));
        table.AddRow("To UTC", FormatDateTime(response.ToUtc));
        table.AddRow("Base points count", response.BasePointsCount.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Quote points count", response.QuotePointsCount.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Matched returns count", response.MatchedReturnsCount.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Pearson correlation", FormatNullableDecimal(response.PearsonCorrelation));

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
            ? value.Value.ToString("N6", CultureInfo.InvariantCulture)
            : "N/A";
    }
}