namespace Orders.Bll.Dtos;

public record ProductDto(long Id, string Name, decimal Price, int Stock);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int? TotalCount);
