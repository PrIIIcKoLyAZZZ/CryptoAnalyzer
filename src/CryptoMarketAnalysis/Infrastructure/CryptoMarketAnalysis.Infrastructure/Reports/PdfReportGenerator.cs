using CryptoMarketAnalysis.Application.Abstractions.Reports;
using CryptoMarketAnalysis.Application.Contracts.Reports;
using CryptoMarketAnalysis.Infrastructure.Reports.Charts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace CryptoMarketAnalysis.Infrastructure.Reports;

public sealed class PdfReportGenerator : IPdfReportGenerator
{
    private const int MaxHistoricalTableRows = 50;
    private const int HistoricalTableEdgeRows = 25;

    public Task<byte[]> GenerateAsync(
        MarketAnalysisReportModel report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        cancellationToken.ThrowIfCancellationRequested();

        QuestPDF.Settings.License = LicenseType.Community;

        byte[] content = Document
            .Create(container =>
            {
                ComposeMainPage(container, report);
                ComposePriceChartPage(container, report);
                ComposeVolumeChartPage(container, report);
            })
            .GeneratePdf();

        return Task.FromResult(content);
    }

    private static void ComposeMainPage(
        IDocumentContainer container,
        MarketAnalysisReportModel report)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(text => text.FontSize(10));

            page.Header()
                .Text("Отчет по анализу криптовалютного рынка")
                .FontSize(20)
                .Bold();

            page.Content()
                .Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Element(container => ComposeMetadata(container, report));
                    column.Item().Element(container => ComposeSummary(container, report));
                    column.Item().Element(container => ComposeHistoricalTable(container, report));
                    column.Item().Element(ComposeNotes);
                });

            ComposeFooter(page);
        });
    }

    private static void ComposePriceChartPage(
        IDocumentContainer container,
        MarketAnalysisReportModel report)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(text => text.FontSize(10));

            page.Header()
                .Text("График цены")
                .FontSize(20)
                .Bold();

            page.Content()
                .Element(container => ComposePriceChart(container, report));

            ComposeFooter(page);
        });
    }

    private static void ComposeVolumeChartPage(
        IDocumentContainer container,
        MarketAnalysisReportModel report)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(text => text.FontSize(10));

            page.Header()
                .Text("График объема торгов")
                .FontSize(20)
                .Bold();

            page.Content()
                .Element(container => ComposeVolumeChart(container, report));

            ComposeFooter(page);
        });
    }

    private static void ComposeFooter(
        PageDescriptor page)
    {
        page.Footer()
            .AlignCenter()
            .Text(text =>
            {
                text.Span("Страница ");
                text.CurrentPageNumber();
                text.Span(" из ");
                text.TotalPages();
            });
    }

    private static void ComposePriceChart(
        IContainer container,
        MarketAnalysisReportModel report)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item()
                .Text($"Динамика цен {report.Symbol} ({report.MarketDataSourceCode ?? "ALL"})")
                .FontSize(14)
                .Bold();

            string svg = PriceChartSvgBuilder.Build(report.Points);

            column.Item().Svg(svg);
        });
    }

    private static void ComposeVolumeChart(
        IContainer container,
        MarketAnalysisReportModel report)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item()
                .Text($"Объем Торгов {report.Symbol} ({report.MarketDataSourceCode ?? "ALL"})")
                .FontSize(14)
                .Bold();

            string svg = VolumeChartSvgBuilder.Build(report.Points);

            column.Item().Svg(svg);
        });
    }

    private static void ComposeMetadata(
        IContainer container,
        MarketAnalysisReportModel report)
    {
        container.Column(column =>
        {
            column.Spacing(4);

            column.Item().Text("Параметры анализа").FontSize(14).Bold();

            column.Item().Text($"Актив: {report.Symbol}");
            column.Item().Text($"Источник данных: {report.MarketDataSourceCode ?? "ALL"}");
            column.Item().Text($"Период: {FormatDateTime(report.FromUtc)} — {FormatDateTime(report.ToUtc)}");
            column.Item().Text($"Дата формирования (UTC): {FormatDateTime(report.GeneratedAtUtc)}");
        });
    }

    private static void ComposeSummary(
        IContainer container,
        MarketAnalysisReportModel report)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            column.Item().Text("Ключевые показатели");

            column.Item().Row(row =>
            {
                row.Spacing(8);

                AddSummaryCard(
                    row,
                    "Начальная цена",
                    FormatNullableDecimal(report.PriceChange.StartPriceUsd),
                    "USD");

                AddSummaryCard(
                    row,
                    "Конечная цена",
                    FormatNullableDecimal(report.PriceChange.EndPriceUsd),
                    "USD");

                AddSummaryCard(
                    row,
                    "Изменение цены",
                    FormatPercent(report.PriceChange.PercentageChange),
                    "процентное изменение");

                AddSummaryCard(
                    row,
                    "Волатильность",
                    FormatPercent(report.Volatility.VolatilityPercent),
                    "не приведена к году");
            });

            column.Item().Row(row =>
            {
                row.Spacing(8);

                AddSummaryCard(
                    row,
                    "Количество точек",
                    report.Points.Count.ToString(CultureInfo.InvariantCulture),
                    "исторических записей");

                AddSummaryCard(
                    row,
                    "Средняя доходность",
                    FormatPercent(report.Volatility.AverageReturnPercent),
                    "за период");

                if (report.Correlation is null)
                {
                    AddSummaryCard(
                        row,
                        "Корреляция",
                        "N/A",
                        "not requested");
                }
                else
                {
                    AddSummaryCard(
                        row,
                        "Корреляция",
                        FormatNullableDecimal(report.Correlation.PearsonCorrelation),
                        $"{report.Correlation.BaseSymbol}/{report.Correlation.QuoteSymbol}");
                }

                AddSummaryCard(
                    row,
                    "Сопоставлено доходностей",
                    report.Correlation?.MatchedReturnsCount.ToString(CultureInfo.InvariantCulture) ?? "N/A",
                    "для расчета корреляции");
            });
        });
    }

    private static void AddSummaryCard(
        RowDescriptor row,
        string title,
        string value,
        string subtitle)
    {
        row.RelativeItem()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(8)
            .Column(column =>
            {
                column.Spacing(3);

                column.Item()
                    .Text(title)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);

                column.Item()
                    .Text(value)
                    .FontSize(13)
                    .Bold();

                column.Item()
                    .Text(subtitle)
                    .FontSize(7)
                    .FontColor(Colors.Grey.Darken1);
            });
    }

    private static string FormatPercent(
        decimal? value)
    {
        return value.HasValue
            ? $"{value.Value.ToString("N2", CultureInfo.InvariantCulture)}%"
            : "N/A";
    }

    private static void ComposeHistoricalTable(
        IContainer container,
        MarketAnalysisReportModel report)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item().Text("Исторические данные");

            if (report.Points.Count > MaxHistoricalTableRows)
            {
                column.Item()
                    .Text($"$\"\"\"\nТаблица сокращена для повышения читаемости отчета.\nПоказаны первые {{HistoricalTableEdgeRows}} и последние {{HistoricalTableEdgeRows}} записей.\nВсего точек: {{report.Points.Count}}.\n\"\"\".")
                    .FontSize(9)
                    .Italic();
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    AddHeaderCell(header, "Дата (UTC)");
                    AddHeaderCell(header, "Цена (USD)");
                    AddHeaderCell(header, "Капитализация (USD)");
                    AddHeaderCell(header, "Объем 24ч (USD)");
                });

                IReadOnlyCollection<MarketAnalysisReportPointDto> visiblePoints =
                    GetVisibleHistoricalPoints(report.Points);

                foreach (MarketAnalysisReportPointDto point in visiblePoints.OrderBy(point => point.TimestampUtc))
                {
                    AddCell(table, FormatDateTime(point.TimestampUtc));
                    AddCell(table, FormatDecimal(point.PriceUsd));
                    AddCell(table, FormatNullableDecimal(point.MarketCapUsd));
                    AddCell(table, FormatNullableDecimal(point.Volume24hUsd));
                }
            });
        });
    }

    private static void ComposeNotes(
    IContainer container)
{
    container.Column(column =>
    {
        column.Spacing(6);

        column.Item()
            .Text("Пояснения")
            .FontSize(14)
            .Bold();

        column.Item()
            .BorderLeft(4)
            .BorderColor(Colors.Grey.Medium)
            .Background(Colors.Grey.Lighten5)
            .Padding(10)
            .Column(notes =>
            {
                notes.Spacing(6);
                notes.Item().Text(
                    "• Капитализация может отсутствовать для данных Binance, так как спотовые API Binance возвращают информацию по торговым парам, а не по капитализации актива.");
                notes.Item().Text(
                    "• Волатильность рассчитана как выборочное стандартное отклонение доходностей за выбранный период и не приведена к годовому значению.");
                notes.Item().Text(
                    "• Корреляция рассчитана по доходностям, сопоставленным по времени наблюдения.");
                notes.Item().Text(
                    "• Для больших периодов историческая таблица автоматически сокращается для сохранения читаемости отчета.");
            });
    });
}

    private static void AddHeaderCell(
        TableCellDescriptor header,
        string text)
    {
        header.Cell()
            .Element(HeaderCellStyle)
            .Text(text)
            .Bold();
    }

    private static void AddCell(
        TableDescriptor table,
        string text)
    {
        table.Cell().Element(CellStyle).Text(text);
    }

    private static IContainer HeaderCellStyle(
        IContainer container)
    {
        return container
            .Border(1)
            .Background(Colors.Grey.Lighten3)
            .Padding(4);
    }

    private static IContainer CellStyle(
        IContainer container)
    {
        return container
            .Border(1)
            .Padding(4);
    }

    private static string FormatDateTime(
        DateTime value)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static string FormatDecimal(
        decimal value)
    {
        return value.ToString("N2", CultureInfo.InvariantCulture);
    }

    private static string FormatNullableDecimal(
        decimal? value)
    {
        return value.HasValue
            ? FormatDecimal(value.Value)
            : "N/A";
    }

    private static IReadOnlyCollection<MarketAnalysisReportPointDto> GetVisibleHistoricalPoints(
        IReadOnlyCollection<MarketAnalysisReportPointDto> points)
    {
        if (points.Count <= MaxHistoricalTableRows)
            return points;

        return points
            .Take(HistoricalTableEdgeRows)
            .Concat(points.TakeLast(HistoricalTableEdgeRows))
            .ToArray();
    }
}