namespace TechTrek.Shared.Common;

/// <summary>
/// Paginated result wrapper for list queries.
/// All list endpoints return this structure.
/// </summary>
public sealed class PagedResult<T>
{
    public IList<T> Items { get; init; } = new List<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Create(IList<T> items, int totalCount, int page, int pageSize)
        => new() { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };

    public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
        => new() { Items = new List<T>(), TotalCount = 0, Page = page, PageSize = pageSize };
}

/// <summary>
/// Pagination parameters - all list queries accept these.
/// </summary>
public sealed record PaginationParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = false;
    public string? Search { get; init; }

    public int Skip => (Page - 1) * PageSize;

    public PaginationParams Validate()
    {
        var page = Math.Max(1, Page);
        var pageSize = Math.Clamp(PageSize, 1, 100);
        return this with { Page = page, PageSize = pageSize };
    }
}
