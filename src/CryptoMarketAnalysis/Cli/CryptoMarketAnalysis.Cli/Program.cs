using CryptoMarketAnalysis.Application;
using CryptoMarketAnalysis.Cli.Commands;
using CryptoMarketAnalysis.Cli.Commands.Analytics;
using CryptoMarketAnalysis.Cli.Commands.Reports;
using CryptoMarketAnalysis.Cli.Infrastructure;
using CryptoMarketAnalysis.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var registrar = new TypeRegistrar(builder.Services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("crypto");

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Show CLI version.");

    config.AddBranch("load", load =>
    {
        load.SetDescription("Load market data.");
        load.AddCommand<LoadCommand>("run")
            .WithDescription("Load market data for a short period.");
        load.AddCommand<LoadBackfillCommand>("backfill")
            .WithDescription("Load market data using date range batching.");
    });

    config.AddBranch("analytics", analytics =>
    {
        analytics.SetDescription("Run analytics commands.");

        analytics.AddCommand<PriceChangeCommand>("price-change")
            .WithDescription("Analyze price change for a symbol.");

        analytics.AddCommand<VolatilityCommand>("volatility")
            .WithDescription("Calculate volatility for a symbol.");

        analytics.AddCommand<CorrelationCommand>("correlation")
            .WithDescription("Calculate Pearson correlation between two symbols.");
    });

    config.AddBranch("report", report =>
    {
        report.SetDescription("Generate reports.");
        report.AddCommand<PdfReportCommand>("pdf")
            .WithDescription("Generate market analysis PDF report.");
    });
});

return await app.RunAsync(args);