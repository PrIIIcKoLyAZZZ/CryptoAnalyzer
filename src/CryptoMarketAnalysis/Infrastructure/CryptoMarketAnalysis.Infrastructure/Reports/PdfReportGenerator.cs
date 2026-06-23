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
                .Text("Crypto Market Analysis Report")
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
                .Text("Price chart")
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
                .Text("Volume chart")
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
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
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
                .Text($"{report.Symbol} price dynamics ({report.MarketDataSourceCode ?? "ALL"})")
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
                .Text($"{report.Symbol} trading volume ({report.MarketDataSourceCode ?? "ALL"})")
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

            column.Item().Text("Metadata").FontSize(14).Bold();

            column.Item().Text($"Symbol: {report.Symbol}");
            column.Item().Text($"Source: {report.MarketDataSourceCode ?? "ALL"}");
            column.Item().Text($"Period: {FormatDateTime(report.FromUtc)} — {FormatDateTime(report.ToUtc)}");
            column.Item().Text($"Generated at UTC: {FormatDateTime(report.GeneratedAtUtc)}");
        });
    }

    private static void ComposeSummary(
        IContainer container,
        MarketAnalysisReportModel report)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item().Text("Summary").FontSize(14).Bold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddRow(table, "Points count", report.Points.Count.ToString(CultureInfo.InvariantCulture));

                AddRow(table, "Start price, USD", FormatNullableDecimal(report.PriceChange.StartPriceUsd));
                AddRow(table, "End price, USD", FormatNullableDecimal(report.PriceChange.EndPriceUsd));
                AddRow(table, "Absolute change, USD", FormatNullableDecimal(report.PriceChange.AbsoluteChangeUsd));
                AddRow(table, "Percentage change, %", FormatNullableDecimal(report.PriceChange.PercentageChange));

                AddRow(table, "Average return, %", FormatNullableDecimal(report.Volatility.AverageReturnPercent));
                AddRow(table, "Volatility, %", FormatNullableDecimal(report.Volatility.VolatilityPercent));

                if (report.Correlation is null)
                {
                    AddRow(table, "Correlation", "Not requested");
                }
                else
                {
                    AddRow(table, "Correlation pair", $"{report.Correlation.BaseSymbol}/{report.Correlation.QuoteSymbol}");
                    AddRow(table, "Matched returns", report.Correlation.MatchedReturnsCount.ToString(CultureInfo.InvariantCulture));
                    AddRow(table, "Pearson correlation", FormatNullableDecimal(report.Correlation.PearsonCorrelation));
                }
            });
        });
    }

    private static void ComposeHistoricalTable(
        IContainer container,
        MarketAnalysisReportModel report)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item().Text("Historical data").FontSize(14).Bold();

            if (report.Points.Count > MaxHistoricalTableRows)
            {
                column.Item()
                    .Text($"Historical table is truncated. Showing first {HistoricalTableEdgeRows} and last {HistoricalTableEdgeRows} rows. Total points: {report.Points.Count}.")
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
                    AddHeaderCell(header, "Timestamp UTC");
                    AddHeaderCell(header, "Price USD");
                    AddHeaderCell(header, "Market Cap USD");
                    AddHeaderCell(header, "Volume 24h USD");
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
            column.Spacing(4);

            column.Item().Text("Notes").FontSize(14).Bold();
            column.Item().Text("MarketCapUsd can be null for Binance because Binance spot endpoints return trading-pair data, not asset capitalization.");
            column.Item().Text("Volatility is calculated as sample standard deviation of period returns and is not annualized.");
            column.Item().Text("Correlation is calculated using returns matched by TimestampUtc.");
            column.Item().Text("Historical table can be truncated for long periods to keep the PDF readable.");
        });
    }

    private static void AddRow(
        TableDescriptor table,
        string name,
        string value)
    {
        AddCell(table, name);
        AddCell(table, value);
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