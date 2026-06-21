namespace FishFarm.Application.Common.Models;

/// <summary>Paginated result wrapper returned by list queries.</summary>
public sealed class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PaginatedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize) => new()
    {
        Items       = items,
        TotalCount  = totalCount,
        PageNumber  = pageNumber,
        PageSize    = pageSize
    };
}
