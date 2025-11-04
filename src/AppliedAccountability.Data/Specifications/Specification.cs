using System.Linq.Expressions;

namespace AppliedAccountability.Data.Specifications;

/// <summary>
/// Base class for specifications implementing the Specification pattern.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <summary>
    /// Gets the filter criteria expression.
    /// </summary>
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    /// <summary>
    /// Gets the list of include expressions (for eager loading).
    /// </summary>
    public List<Expression<Func<T, object>>> Includes { get; } = new();

    /// <summary>
    /// Gets the list of include strings (for eager loading by property name).
    /// </summary>
    public List<string> IncludeStrings { get; } = new();

    /// <summary>
    /// Gets the order by expression.
    /// </summary>
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <summary>
    /// Gets the order by descending expression.
    /// </summary>
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <summary>
    /// Gets the number of records to skip.
    /// </summary>
    public int? Skip { get; private set; }

    /// <summary>
    /// Gets the number of records to take.
    /// </summary>
    public int? Take { get; private set; }

    /// <summary>
    /// Gets whether tracking is enabled.
    /// </summary>
    public bool IsTracking { get; private set; } = true;

    /// <summary>
    /// Gets whether to ignore query filters (like soft delete filter).
    /// </summary>
    public bool IgnoreQueryFilters { get; private set; }

    /// <summary>
    /// Adds filter criteria.
    /// </summary>
    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Adds an include expression for eager loading.
    /// </summary>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds an include string for eager loading by property name.
    /// </summary>
    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Adds order by ascending.
    /// </summary>
    protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Adds order by descending.
    /// </summary>
    protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Applies paging.
    /// </summary>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Disables change tracking for read-only queries.
    /// </summary>
    protected void AsNoTracking()
    {
        IsTracking = false;
    }

    /// <summary>
    /// Ignores global query filters (like soft delete).
    /// </summary>
    protected void IgnoreFilters()
    {
        IgnoreQueryFilters = true;
    }
}
