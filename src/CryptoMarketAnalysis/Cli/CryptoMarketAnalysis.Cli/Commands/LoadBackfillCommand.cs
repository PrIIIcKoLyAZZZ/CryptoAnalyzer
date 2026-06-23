using CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;
using CryptoMarketAnalysis.Cli.Helpers;
using CryptoMarketAnalysis.Cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;

namespace CryptoMarketAnalysis.Cli.Commands;

public sealed class LoadBackfillCommand : AsyncCommand<BackfillCommandSettings>
{
    private readonly ILoadMarketDataUseCase _useCase;

    public LoadBackfillCommand(
        ILoadMarketDataUseCase useCase)
    {
        _useCase = useCase;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        BackfillCommandSettings settings,
        CancellationToken cancellationToken)
    {
        string[] symbols = settings.Symbols
            .Select(symbol => symbol.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] sources = settings.Sources
            .Select(source => source.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        IReadOnlyCollection<DateRangeBatch> batches = DateRangeBatchSplitter.Split(
            settings.FromUtc,
            settings.ToUtc,
            settings.BatchDays);

        AnsiConsole.MarkupLine("[bold]Backfill started[/]");
        AnsiConsole.MarkupLine($"Symbols: [green]{string.Join(", ", symbols)}[/]");
        AnsiConsole.MarkupLine($"Sources: [green]{string.Join(", ", sources)}[/]");
        AnsiConsole.MarkupLine($"Period: [green]{settings.FromUtc:yyyy-MM-dd} — {settings.ToUtc:yyyy-MM-dd}[/]");
        AnsiConsole.MarkupLine($"Batch days: [green]{settings.BatchDays}[/]");
        AnsiConsole.MarkupLine($"Total batches per source: [green]{batches.Count}[/]");
        AnsiConsole.WriteLine();

        var results = new List<BackfillResult>();

        await AnsiConsole
            .Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async progress =>
            {
                int totalSteps = sources.Length * batches.Count;

                ProgressTask task = progress.AddTask(
                    "Backfill loading",
                    maxValue: totalSteps);

                foreach (string source in sources)
                {
                    foreach (DateRangeBatch batch in batches)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        task.Description =
                            $"{source}: {batch.FromUtc:yyyy-MM-dd} — {batch.ToUtc:yyyy-MM-dd}";

                        try
                        {
                            LoadMarketDataResponse response =
                                await _useCase.ExecuteAsync(
                                    new LoadMarketDataRequest(
                                        Symbols: symbols,
                                        FromUtc: batch.FromUtc,
                                        ToUtc: batch.ToUtc,
                                        MarketDataSourceCode: source),
                                    cancellationToken);

                            results.AddRange(
                                response.Results.Select(result => new BackfillResult(
                                    Source: source,
                                    Symbol: result.Symbol,
                                    Batches: 1,
                                    Loaded: result.LoadedPointsCount,
                                    Skipped: result.SkippedDuplicatesCount,
                                    Error: result.Error)));
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            foreach (string symbol in symbols)
                            {
                                results.Add(new BackfillResult(
                                    Source: source,
                                    Symbol: symbol,
                                    Batches: 1,
                                    Loaded: 0,
                                    Skipped: 0,
                                    Error: exception.Message));
                            }
                        }

                        task.Increment(1);
                    }
                }
            });

        WriteSummary(results);

        return 0;
    }

    private static void WriteSummary(
        IReadOnlyCollection<BackfillResult> results)
    {
        var groupedResults = results
            .GroupBy(result => new
            {
                result.Source,
                result.Symbol,
            })
            .Select(group => new
            {
                group.Key.Source,
                group.Key.Symbol,
                Batches = group.Sum(result => result.Batches),
                Loaded = group.Sum(result => result.Loaded),
                Skipped = group.Sum(result => result.Skipped),
                Errors = group.Count(result => !string.IsNullOrWhiteSpace(result.Error)),
                Error = string.Join("; ", group
                    .Where(result => !string.IsNullOrWhiteSpace(result.Error))
                    .Select(result => result.Error)
                    .Distinct()),
            })
            .OrderBy(result => result.Source)
            .ThenBy(result => result.Symbol)
            .ToArray();

        int totalLoaded = groupedResults.Sum(result => result.Loaded);
        int totalSkipped = groupedResults.Sum(result => result.Skipped);
        int totalErrors = groupedResults.Sum(result => result.Errors);

        var panel = new Panel($"""
        Status: {(totalErrors == 0 ? "[green]Success[/]" : "[yellow]Completed with errors[/]")}
        Loaded points: [bold]{totalLoaded}[/]
        Skipped duplicates: [bold]{totalSkipped}[/]
        Errors: [bold]{totalErrors}[/]
        """)
        {
            Header = new PanelHeader("Backfill summary"),
            Border = BoxBorder.Rounded,
        };

        AnsiConsole.Write(panel);

        Table table = new Table()
            .Title("Backfill results")
            .Border(TableBorder.Rounded)
            .AddColumn("Source")
            .AddColumn("Symbol")
            .AddColumn("Batches")
            .AddColumn("Loaded")
            .AddColumn("Skipped")
            .AddColumn("Errors")
            .AddColumn("Error");

        foreach (var result in groupedResults)
        {
            table.AddRow(
                result.Source,
                result.Symbol,
                result.Batches.ToString(CultureInfo.InvariantCulture),
                result.Loaded.ToString(CultureInfo.InvariantCulture),
                result.Skipped.ToString(CultureInfo.InvariantCulture),
                result.Errors.ToString(CultureInfo.InvariantCulture),
                string.IsNullOrWhiteSpace(result.Error) ? "OK" : result.Error);
        }

        AnsiConsole.Write(table);
    }

    private sealed record BackfillResult(
        string Source,
        string Symbol,
        int Batches,
        int Loaded,
        int Skipped,
        string? Error);
}