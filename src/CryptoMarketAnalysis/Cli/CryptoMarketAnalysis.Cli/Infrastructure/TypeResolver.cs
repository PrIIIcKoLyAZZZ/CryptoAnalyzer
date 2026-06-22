using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace CryptoMarketAnalysis.Cli.Infrastructure;

public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public TypeResolver(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? Resolve(Type? type)
    {
        return type is null
            ? null
            : _serviceProvider.GetService(type);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}