namespace BeautyStore.Api.Catalog.Dtos;

public sealed record PagedResult<T>(
    IList<T> Items,
    int      Page,
    int      PageSize,
    int      TotalCount,
    int      TotalPages);
