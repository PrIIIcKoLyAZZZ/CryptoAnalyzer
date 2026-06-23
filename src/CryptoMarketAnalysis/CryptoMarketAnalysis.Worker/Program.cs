using CryptoMarketAnalysis.Application;
using CryptoMarketAnalysis.Infrastructure;
using CryptoMarketAnalysis.Worker.Options;
using CryptoMarketAnalysis.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

string contentRoot = AppContext.BaseDirectory;

HostApplicationBuilderSettings settings = new()
{
    Args = args,
    ContentRootPath = contentRoot,
};

HostApplicationBuilder builder = Host.CreateApplicationBuilder(settings);

builder.Configuration
    .SetBasePath(contentRoot)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<MarketDataLoadOptions>(
    builder.Configuration.GetSection(MarketDataLoadOptions.SectionName));

builder.Services.AddHostedService<DailyMarketDataLoadWorker>();

IHost host = builder.Build();

await host.RunAsync();