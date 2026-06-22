using CryptoMarketAnalysis.Infrastructure.Persistence;
using CryptoMarketAnalysis.Infrastructure.Persistence.Seed;
using System.Text.Json.Serialization;

namespace CryptoMarketAnalysis.Api.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiPresentation(
        this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter());
        });

        return services;
    }

    public static IEndpointRouteBuilder MapApiEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapControllers();
        return app;
    }

    public static async Task SeedDatabaseAsync(
        this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        CryptoMarketAnalysisDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<CryptoMarketAnalysisDbContext>();

        await DatabaseSeeder.SeedAsync(dbContext);
    }
}