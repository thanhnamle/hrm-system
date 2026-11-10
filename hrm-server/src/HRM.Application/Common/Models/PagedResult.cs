using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Common.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>Implemented by list queries. Page numbers start at 1.</summary>
public interface IPagedQuery
{
    int Page { get; }
    int PageSize { get; }
}

public abstract class PagedQueryValidator<T> : AbstractValidator<T> where T : IPagedQuery
{
    public const int MaxPageSize = 100;

    protected PagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}

public static class PagingExtensions
{
    /// <summary>The query must already be ordered, otherwise pages can overlap.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, IPagedQuery paging, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, paging.Page, paging.PageSize, total);
    }
}
