using CryptoMarketAnalysis.Api.Extensions;
using CryptoMarketAnalysis.Application;
using CryptoMarketAnalysis.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiPresentation();

WebApplication app = builder.Build();

await app.SeedDatabaseAsync();

app.UseApiExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapApiEndpoints();

await app.RunAsync();