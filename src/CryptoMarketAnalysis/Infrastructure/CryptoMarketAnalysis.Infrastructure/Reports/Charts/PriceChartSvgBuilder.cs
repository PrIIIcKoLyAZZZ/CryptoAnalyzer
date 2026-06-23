using CryptoMarketAnalysis.Application.Contracts.Reports;
using System.Globalization;
using System.Text;

namespace CryptoMarketAnalysis.Infrastructure.Reports.Charts;

internal static class PriceChartSvgBuilder
{
    private const int Width = 700;
    private const int Height = 220;
    private const int PaddingLeft = 70;
    private const int PaddingRight = 30;
    private const int PaddingTop = 25;
    private const int PaddingBottom = 45;

    public static string Build(
        IReadOnlyCollection<MarketAnalysisReportPointDto> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        MarketAnalysisReportPointDto[] orderedPoints = points
            .OrderBy(point => point.TimestampUtc)
            .ToArray();

        if (orderedPoints.Length < 2)
            return BuildEmptyChart("Недостаточно данных для построения графика цены.");

        decimal minPrice = orderedPoints.Min(point => point.PriceUsd);
        decimal maxPrice = orderedPoints.Max(point => point.PriceUsd);

        if (minPrice == maxPrice)
        {
            minPrice -= 1;
            maxPrice += 1;
        }

        var plotPoints = new List<string>();

        for (int index = 0; index < orderedPoints.Length; index++)
        {
            double x = MapX(
                index,
                orderedPoints.Length);

            double y = MapY(
                orderedPoints[index].PriceUsd,
                minPrice,
                maxPrice);

            plotPoints.Add(
                $"{FormatInvariant(x)},{FormatInvariant(y)}");
        }

        string firstDate = orderedPoints.First().TimestampUtc.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        string lastDate = orderedPoints.Last().TimestampUtc.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        var svg = new StringBuilder();

        svg.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{Width}" height="{Height}" viewBox="0 0 {Width} {Height}">""");
        svg.AppendLine($"""<rect x="0" y="0" width="{Width}" height="{Height}" fill="white"/>""");

        AppendGrid(svg);
        AppendAxes(svg);
        AppendYAxisLabels(svg, minPrice, maxPrice);
        AppendXAxisLabels(svg, firstDate, lastDate);

        svg.AppendLine($"""<polyline points="{string.Join(' ', plotPoints)}" fill="none" stroke="#2563eb" stroke-width="3" stroke-linejoin="round" stroke-linecap="round"/>""");

        AppendPoint(svg, plotPoints.First(), "#16a34a");
        AppendPoint(svg, plotPoints.Last(), "#dc2626");

        svg.AppendLine("""<text x="400" y="18" text-anchor="middle" font-size="14" font-family="Arial" font-weight="bold" fill="#111827">Цена (USD)</text>""");
        svg.AppendLine("</svg>");

        return svg.ToString();
    }

    private static string BuildEmptyChart(
        string message)
    {
        return $"""
        <svg xmlns="http://www.w3.org/2000/svg" width="{Width}" height="{Height}" viewBox="0 0 {Width} {Height}">
            <rect x="0" y="0" width="{Width}" height="{Height}" fill="white"/>
            <rect x="1" y="1" width="{Width - 2}" height="{Height - 2}" fill="none" stroke="#d1d5db"/>
            <text x="{Width / 2}" y="{Height / 2}" text-anchor="middle" font-size="14" font-family="Arial" fill="#6b7280">{EscapeXml(message)}</text>
        </svg>
        """;
    }

    private static void AppendGrid(
        StringBuilder svg)
    {
        int plotLeft = PaddingLeft;
        int plotRight = Width - PaddingRight;
        int plotTop = PaddingTop;
        int plotBottom = Height - PaddingBottom;

        for (int index = 0; index <= 4; index++)
        {
            double y = plotTop + ((plotBottom - plotTop) / 4.0 * index);

            svg.AppendLine(
                $"""<line x1="{plotLeft}" y1="{FormatInvariant(y)}" x2="{plotRight}" y2="{FormatInvariant(y)}" stroke="#e5e7eb" stroke-width="1"/>""");
        }
    }

    private static void AppendAxes(
        StringBuilder svg)
    {
        int plotLeft = PaddingLeft;
        int plotRight = Width - PaddingRight;
        int plotTop = PaddingTop;
        int plotBottom = Height - PaddingBottom;

        svg.AppendLine($"""<line x1="{plotLeft}" y1="{plotTop}" x2="{plotLeft}" y2="{plotBottom}" stroke="#374151" stroke-width="1.5"/>""");
        svg.AppendLine($"""<line x1="{plotLeft}" y1="{plotBottom}" x2="{plotRight}" y2="{plotBottom}" stroke="#374151" stroke-width="1.5"/>""");
    }

    private static void AppendYAxisLabels(
        StringBuilder svg,
        decimal minPrice,
        decimal maxPrice)
    {
        int plotTop = PaddingTop;
        int plotBottom = Height - PaddingBottom;

        for (int index = 0; index <= 4; index++)
        {
            decimal value = maxPrice - ((maxPrice - minPrice) / 4 * index);
            double y = plotTop + ((plotBottom - plotTop) / 4.0 * index) + 4;

            svg.AppendLine(
                $"""<text x="62" y="{FormatInvariant(y)}" text-anchor="end" font-size="11" font-family="Arial" fill="#4b5563">{EscapeXml(FormatMoney(value))}</text>""");
        }
    }

    private static void AppendXAxisLabels(
        StringBuilder svg,
        string firstDate,
        string lastDate)
    {
        int plotLeft = PaddingLeft;
        int plotRight = Width - PaddingRight;
        int plotBottom = Height - PaddingBottom;

        svg.AppendLine(
            $"""<text x="{plotLeft}" y="{plotBottom + 25}" text-anchor="start" font-size="11" font-family="Arial" fill="#4b5563">{EscapeXml(firstDate)}</text>""");

        svg.AppendLine(
            $"""<text x="{plotRight}" y="{plotBottom + 25}" text-anchor="end" font-size="11" font-family="Arial" fill="#4b5563">{EscapeXml(lastDate)}</text>""");
    }

    private static void AppendPoint(
        StringBuilder svg,
        string point,
        string color)
    {
        string[] coordinates = point.Split(',');

        svg.AppendLine(
            $"""<circle cx="{coordinates[0]}" cy="{coordinates[1]}" r="4" fill="{color}"/>""");
    }

    private static double MapX(
        int index,
        int pointsCount)
    {
        double plotWidth = Width - PaddingLeft - PaddingRight;

        return PaddingLeft + (plotWidth * index / (pointsCount - 1));
    }

    private static double MapY(
        decimal price,
        decimal minPrice,
        decimal maxPrice)
    {
        double plotHeight = Height - PaddingTop - PaddingBottom;
        decimal range = maxPrice - minPrice;

        decimal normalized = (price - minPrice) / range;

        return PaddingTop + plotHeight - ((double)normalized * plotHeight);
    }

    private static string FormatMoney(
        decimal value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatInvariant(
        double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string EscapeXml(
        string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}