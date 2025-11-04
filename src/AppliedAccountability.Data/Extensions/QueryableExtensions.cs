using AppliedAccountability.Data.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AppliedAccountability.Data.Extensions;

/// <summary>
/// Extension methods for IQueryable to support pagination.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Converts a queryable to a paged result.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Applies pagination to a queryable.
    /// </summary>
    public static IQueryable<T> Paginate<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize)
    {
        return query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }
}
