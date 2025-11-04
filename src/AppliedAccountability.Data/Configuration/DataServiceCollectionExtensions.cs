using AppliedAccountability.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppliedAccountability.Data.Configuration;

/// <summary>
/// Extension methods for registering Data services.
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Adds Unit of Work pattern support.
    /// </summary>
    public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            return new UnitOfWork.UnitOfWork(context);
        });

        return services;
    }

    /// <summary>
    /// Adds repository pattern support for a specific entity.
    /// </summary>
    public static IServiceCollection AddRepository<TEntity, TKey, TContext>(this IServiceCollection services)
        where TEntity : class, Entities.IEntity<TKey>
        where TKey : IEquatable<TKey>
        where TContext : DbContext
    {
        services.AddScoped<Repositories.IRepository<TEntity, TKey>>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            return new Repositories.Repository<TEntity, TKey>(context);
        });

        return services;
    }
}
