using KioskRewards.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KioskRewards.Infrastructure.Common;

/// <summary>
/// One place that does the Count + clamp + Skip/Take dance, so every paginated EF query looks the
/// same instead of each service reimplementing it slightly differently.
/// </summary>
public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedQuery pagedQuery,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, pagedQuery.Page);
        var pageSize = Math.Max(1, pagedQuery.PageSize);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
