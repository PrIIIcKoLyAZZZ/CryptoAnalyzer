namespace CryptoMarketAnalysis.Application.Contracts.Common;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int Page,
    int PageSize);