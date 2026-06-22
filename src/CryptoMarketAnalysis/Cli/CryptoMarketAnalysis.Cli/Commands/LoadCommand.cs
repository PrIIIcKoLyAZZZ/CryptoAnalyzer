using CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;
using CryptoMarketAnalysis.Cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CryptoMarketAnalysis.Cli.Commands;

public sealed class LoadCommand : AsyncCommand<LoadCommandSettings>
{
    private readonly ILoadMarketDataUseCase _useCase;

    public LoadCommand(
        ILoadMarketDataUseCase useCase)
    {
        _useCase = useCase;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        LoadCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var fromUtc = DateTime.SpecifyKind(settings.FromUtc.Date, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(settings.ToUtc.Date, DateTimeKind.Utc);

        LoadMarketDataResponse response = await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Loading market data...", async _ =>
                await _useCase.ExecuteAsync(
                    new LoadMarketDataRequest(
                        Symbols: settings.Symbols,
                        FromUtc: fromUtc,
                        ToUtc: toUtc,
                        MarketDataSourceCode: settings.MarketDataSourceCode),
                    cancellationToken));

        WriteSummary(response);
        WriteResults(response);

        return response.Status == LoadMarketDataStatus.Failed
            ? 1
            : 0;
    }

    private static void WriteSummary(
        LoadMarketDataResponse response)
    {
        var panel = new Panel($"""
        Status: [bold]{response.Status}[/]
        Requested symbols: [bold]{response.RequestedSymbolsCount}[/]
        Loaded points: [green]{response.LoadedPointsCount}[/]
        Skipped duplicates: [yellow]{response.SkippedDuplicatesCount}[/]
        """)
        {
            Header = new PanelHeader("Load summary"),
            Border = BoxBorder.Rounded,
        };

        AnsiConsole.Write(panel);
    }

    private static void WriteResults(
        LoadMarketDataResponse response)
    {
        Table table = new Table()
            .Title("Load results")
            .Border(TableBorder.Rounded)
            .AddColumn("Symbol")
            .AddColumn("Source")
            .AddColumn("Loaded")
            .AddColumn("Skipped")
            .AddColumn("Error");

        foreach (LoadMarketDataSymbolResult result in response.Results)
        {
            table.AddRow(
                result.Symbol,
                result.MarketDataSourceCode,
                result.LoadedPointsCount.ToString(),
                result.SkippedDuplicatesCount.ToString(),
                result.Error is null ? "[green]OK[/]" : $"[red]{Markup.Escape(result.Error)}[/]");
        }

        AnsiConsole.Write(table);
    }
}