using CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;
using CryptoMarketAnalysis.Worker.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CryptoMarketAnalysis.Worker.Services;

public sealed class DailyMarketDataLoadWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<MarketDataLoadOptions> _options;
    private readonly ILogger<DailyMarketDataLoadWorker> _logger;

    public DailyMarketDataLoadWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<MarketDataLoadOptions> options,
        ILogger<DailyMarketDataLoadWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        MarketDataLoadOptions options = _options.Value;

        if (!ValidateOptions(options))
            return;

        if (!options.Enabled)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Daily market data load worker is disabled.");
            }

            return;
        }

        var interval = TimeSpan.FromMinutes(options.IntervalMinutes);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Daily market data load worker started. IntervalMinutes: {IntervalMinutes}.",
                options.IntervalMinutes);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteLoadAsync(options, stoppingToken);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Next daily market data load will start in {IntervalMinutes} minutes.",
                    options.IntervalMinutes);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ExecuteLoadAsync(
        MarketDataLoadOptions options,
        CancellationToken cancellationToken)
    {
        DateTime toUtc = DateTime.UtcNow.Date;
        DateTime fromUtc = toUtc.AddDays(-options.DaysBack);
        string symbols = string.Join(", ", options.Symbols);
        string source = options.MarketDataSourceCode ?? "ALL";

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Daily market data load started. Period: {FromUtc} — {ToUtc}. Symbols: {Symbols}. Source: {Source}.",
                fromUtc,
                toUtc,
                symbols,
                source);
        }

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();

            ILoadMarketDataUseCase useCase =
                scope.ServiceProvider.GetRequiredService<ILoadMarketDataUseCase>();

            var request = new LoadMarketDataRequest(
                Symbols: options.Symbols,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                MarketDataSourceCode: options.MarketDataSourceCode);

            LoadMarketDataResponse response = await useCase.ExecuteAsync(
                request,
                cancellationToken);

            int loaded = response.Results.Sum(result => result.LoadedPointsCount);
            int skipped = response.Results.Sum(result => result.SkippedDuplicatesCount);
            int errors = response.Results.Count(result => !string.IsNullOrWhiteSpace(result.Error));

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Daily market data load completed. LoadedPointsCount: {Loaded}. SkippedDuplicatesCount: {Skipped}. Errors: {Errors}.",
                    loaded,
                    skipped,
                    errors);
            }

            foreach (LoadMarketDataSymbolResult result in response.Results)
            {
                string resultError = result.Error ?? string.Empty;

                if (string.IsNullOrWhiteSpace(resultError))
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                            "Scheduled load result. Symbol: {Symbol}. Source: {Source}. Loaded: {Loaded}. Skipped: {Skipped}.",
                            result.Symbol,
                            result.MarketDataSourceCode,
                            result.LoadedPointsCount,
                            result.SkippedDuplicatesCount);
                    }
                }
                else if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        "Scheduled load result with error. Symbol: {Symbol}. Source: {Source}. Error: {Error}.",
                        result.Symbol,
                        result.MarketDataSourceCode,
                        resultError);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Daily market data load worker is stopping.");
            }
        }
        catch (Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    exception,
                    "Daily market data load failed.");
            }
        }
    }

    private bool ValidateOptions(
        MarketDataLoadOptions options)
    {
        if (options.IntervalMinutes <= 0)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    "Worker MarketDataLoad configuration is invalid: IntervalMinutes must be greater than zero.");
            }

            return false;
        }

        if (options.DaysBack <= 0)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    "Worker MarketDataLoad configuration is invalid: DaysBack must be greater than zero.");
            }

            return false;
        }

        if (options.Symbols.Length == 0)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    "Worker MarketDataLoad configuration is invalid: Symbols must not be empty.");
            }

            return false;
        }

        foreach (string symbol in options.Symbols)
        {
            if (!string.IsNullOrWhiteSpace(symbol))
                continue;

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    "Worker MarketDataLoad configuration is invalid: Symbols must not contain empty values.");
            }

            return false;
        }

        return true;
    }
}