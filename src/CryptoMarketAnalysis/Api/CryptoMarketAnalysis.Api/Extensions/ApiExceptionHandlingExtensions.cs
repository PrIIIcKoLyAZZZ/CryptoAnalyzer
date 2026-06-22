using CryptoMarketAnalysis.Api.Middleware;

namespace CryptoMarketAnalysis.Api.Extensions;

public static class ApiExceptionHandlingExtensions
{
    public static IApplicationBuilder UseApiExceptionHandling(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiExceptionHandlingMiddleware>();
    }
}