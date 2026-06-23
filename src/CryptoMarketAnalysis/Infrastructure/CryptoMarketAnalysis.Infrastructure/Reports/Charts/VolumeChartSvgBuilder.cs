using CryptoMarketAnalysis.Application.Contracts.Reports;
using System.Globalization;
using System.Text;

namespace CryptoMarketAnalysis.Infrastructure.Reports.Charts;

internal static class VolumeChartSvgBuilder
{
    private const int Width = 500;
    private const int Height = 220;
    private const int PaddingLeft = 80;
    private const int PaddingRight = 30;
    private const int PaddingTop = 25;
    private const int PaddingBottom = 45;

    public static string Build(
        IReadOnlyCollection<MarketAnalysisReportPointDto> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        ChartPoint[] orderedPoints = points
            .Where(point => point.Volume24hUsd.HasValue)
            .Select(point => new ChartPoint(
                point.TimestampUtc,
                point.Volume24hUsd.GetValueOrDefault()))
            .OrderBy(point => point.TimestampUtc)
            .ToArray();

        if (orderedPoints.Length < 2)
            return BuildEmptyChart("Данные по объему торгов отсутствуют для выбранного периода.");

        decimal minVolume = orderedPoints.Min(point => point.Volume);
        decimal maxVolume = orderedPoints.Max(point => point.Volume);

        if (minVolume == maxVolume)
        {
            minVolume = 0;
            maxVolume += 1;
        }

        var plotPoints = new List<string>();

        for (int index = 0; index < orderedPoints.Length; index++)
        {
            double x = MapX(index, orderedPoints.Length);
            double y = MapY(orderedPoints[index].Volume, minVolume, maxVolume);

            plotPoints.Add($"{FormatInvariant(x)},{FormatInvariant(y)}");
        }

        string firstDate = orderedPoints.First().TimestampUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string lastDate = orderedPoints.Last().TimestampUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var svg = new StringBuilder();

        svg.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{Width}" height="{Height}" viewBox="0 0 {Width} {Height}">""");
        svg.AppendLine($"""<rect x="0" y="0" width="{Width}" height="{Height}" fill="white"/>""");

        AppendGrid(svg);
        AppendAxes(svg);
        AppendYAxisLabels(svg, minVolume, maxVolume);
        AppendXAxisLabels(svg, firstDate, lastDate);

        svg.AppendLine($"""<polyline points="{string.Join(' ', plotPoints)}" fill="none" stroke="#7c3aed" stroke-width="3" stroke-linejoin="round" stroke-linecap="round"/>""");

        svg.AppendLine($"""<text x="{Width / 2}" y="18" text-anchor="middle" font-size="14" font-family="Arial" font-weight="bold" fill="#111827">Объем за 24 часа (USD)</text>""");
        svg.AppendLine($"""<text x="{PaddingLeft}" y="{Height - 8}" text-anchor="start" font-size="10" font-family="Arial" fill="#4b5563">Минимум: {EscapeXml(FormatCompactMoney(minVolume))}</text>""");
        svg.AppendLine($"""<text x="{Width - PaddingRight}" y="{Height - 8}" text-anchor="end" font-size="10" font-family="Arial" fill="#4b5563">Максимум: {EscapeXml(FormatCompactMoney(maxVolume))}</text>""");

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
            <text x="{Width / 2}" y="{Height / 2}" text-anchor="middle" font-size="13" font-family="Arial" fill="#6b7280">{EscapeXml(message)}</text>
        </svg>
        """;
    }

    private static void AppendGrid(StringBuilder svg)
    {
        int plotLeft = PaddingLeft;
        int plotRight = Width - PaddingRight;
        int plotTop = PaddingTop;
        int plotBottom = Height - PaddingBottom;

        for (int index = 0; index <= 4; index++)
        {
            double y = plotTop + ((plotBottom - plotTop) / 4.0 * index);

            svg.AppendLine($"""<line x1="{plotLeft}" y1="{FormatInvariant(y)}" x2="{plotRight}" y2="{FormatInvariant(y)}" stroke="#e5e7eb" stroke-width="1"/>""");
        }
    }

    private static void AppendAxes(StringBuilder svg)
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
        decimal minVolume,
        decimal maxVolume)
    {
        int plotTop = PaddingTop;
        int plotBottom = Height - PaddingBottom;

        for (int index = 0; index <= 4; index++)
        {
            decimal value = maxVolume - ((maxVolume - minVolume) / 4 * index);
            double y = plotTop + ((plotBottom - plotTop) / 4.0 * index) + 4;

            svg.AppendLine($"""<text x="72" y="{FormatInvariant(y)}" text-anchor="end" font-size="10" font-family="Arial" fill="#4b5563">{EscapeXml(FormatCompactMoney(value))}</text>""");
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

        svg.AppendLine($"""<text x="{plotLeft}" y="{plotBottom + 25}" text-anchor="start" font-size="11" font-family="Arial" fill="#4b5563">{EscapeXml(firstDate)}</text>""");
        svg.AppendLine($"""<text x="{plotRight}" y="{plotBottom + 25}" text-anchor="end" font-size="11" font-family="Arial" fill="#4b5563">{EscapeXml(lastDate)}</text>""");
    }

    private static double MapX(
        int index,
        int pointsCount)
    {
        double plotWidth = Width - PaddingLeft - PaddingRight;

        return PaddingLeft + (plotWidth * index / (pointsCount - 1));
    }

    private static double MapY(
        decimal value,
        decimal minValue,
        decimal maxValue)
    {
        double plotHeight = Height - PaddingTop - PaddingBottom;
        decimal range = maxValue - minValue;
        decimal normalized = (value - minValue) / range;

        return PaddingTop + plotHeight - ((double)normalized * plotHeight);
    }

    private static string FormatCompactMoney(decimal value)
    {
        if (value >= 1_000_000_000)
            return $"{(value / 1_000_000_000).ToString("N1", CultureInfo.InvariantCulture)}B";

        if (value >= 1_000_000)
            return $"{(value / 1_000_000).ToString("N1", CultureInfo.InvariantCulture)}M";

        if (value >= 1_000)
            return $"{(value / 1_000).ToString("N1", CultureInfo.InvariantCulture)}K";

        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatInvariant(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    private sealed record ChartPoint(
        DateTime TimestampUtc,
        decimal Volume);
}