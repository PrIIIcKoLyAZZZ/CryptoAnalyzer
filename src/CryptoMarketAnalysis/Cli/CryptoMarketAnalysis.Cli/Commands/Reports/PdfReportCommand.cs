using CryptoMarketAnalysis.Application.Contracts.Reports;
using CryptoMarketAnalysis.Cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CryptoMarketAnalysis.Cli.Commands.Reports;

public sealed class PdfReportCommand : AsyncCommand<PdfReportCommandSettings>
{
    private readonly IGenerateMarketAnalysisReportUseCase _useCase;

    public PdfReportCommand(
        IGenerateMarketAnalysisReportUseCase useCase)
    {
        _useCase = useCase;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        PdfReportCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var fromUtc = DateTime.SpecifyKind(settings.FromUtc.Date, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(settings.ToUtc.Date, DateTimeKind.Utc);

        GenerateMarketAnalysisReportResponse response = await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Generating PDF report...", async _ =>
                await _useCase.ExecuteAsync(
                    new GenerateMarketAnalysisReportRequest(
                        Symbol: settings.Symbol,
                        FromUtc: fromUtc,
                        ToUtc: toUtc,
                        MarketDataSourceCode: settings.MarketDataSourceCode,
                        CorrelationSymbol: settings.CorrelationSymbol,
                        OutputPath: settings.OutputPath),
                    cancellationToken));

        string outputPath = ResolveOutputPath(
            settings.OutputPath,
            response.FileName);

        string? directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(
            outputPath,
            response.Content,
            cancellationToken);

        WriteResult(
            outputPath,
            response.ContentType,
            response.Content.Length,
            response.PointsCount);

        return 0;
    }

    private static string ResolveOutputPath(
        string? outputPath,
        string fileName)
    {
        if (!string.IsNullOrWhiteSpace(outputPath))
            return outputPath;

        return Path.Combine(
            Directory.GetCurrentDirectory(),
            fileName);
    }

    private static void WriteResult(
        string outputPath,
        string contentType,
        int fileSizeBytes,
        int pointsCount)
    {
        var panel = new Panel($"""
        File: [green]{Markup.Escape(Path.GetFullPath(outputPath))}[/]
        Content-Type: [bold]{contentType}[/]
        Size: [bold]{fileSizeBytes} bytes[/]
        Points count: [bold]{pointsCount}[/]
        """)
        {
            Header = new PanelHeader("PDF report created"),
            Border = BoxBorder.Rounded,
        };

        AnsiConsole.Write(panel);
    }
}