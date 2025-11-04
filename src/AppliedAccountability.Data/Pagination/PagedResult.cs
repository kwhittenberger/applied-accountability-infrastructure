namespace AppliedAccountability.Data.Pagination;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">Type of items in the result.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Indicates whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Indicates whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Index of the first item on this page (1-based).
    /// </summary>
    public int FirstItemOnPage => (PageNumber - 1) * PageSize + 1;

    /// <summary>
    /// Index of the last item on this page (1-based).
    /// </summary>
    public int LastItemOnPage => Math.Min(PageNumber * PageSize, TotalCount);

    /// <summary>
    /// Creates a new paged result.
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Creates a new paged result with items.
    /// </summary>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    public static PagedResult<T> Empty(int pageNumber, int pageSize)
    {
        return new PagedResult<T>(Array.Empty<T>(), 0, pageNumber, pageSize);
    }
}
