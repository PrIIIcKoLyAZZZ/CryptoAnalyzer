using CryptoMarketAnalysis.Application.Contracts.Exchanges;
using CryptoMarketAnalysis.Domain.Entities;

namespace CryptoMarketAnalysis.Application.Mapping;

public static class ExchangeMappings
{
    public static ExchangeDto ToDto(this Exchange exchange)
    {
        ArgumentNullException.ThrowIfNull(exchange);

        return new ExchangeDto(
            Id: exchange.Id,
            Code: exchange.Code.Value,
            Name: exchange.Name,
            IsActive: exchange.IsActive);
    }
}