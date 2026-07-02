using KioskRewards.Application.Abstractions;
using KioskRewards.Infrastructure.Persistence;
using KioskRewards.Infrastructure.Seeding;
using KioskRewards.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KioskRewards.Infrastructure.DependencyInjection;

/// <summary>
/// One place to wire up the loyalty Infrastructure stack into whatever host is using it.
/// </summary>
public static class InfrastructureServiceRegistration
{
    /// Registers everything with a plain connection string, mostly handy for tests
    public static IServiceCollection AddLoyaltyInfrastructure(this IServiceCollection services, string connectionString)
        => services.AddLoyaltyInfrastructure(_ => connectionString);

    /// <summary>
    /// Same as above, but resolves the connection string lazily from the service provider, so the
    /// host can build the real SQLite path at runtime instead of relying on |DataDirectory|.
    /// </summary>
    public static IServiceCollection AddLoyaltyInfrastructure(this IServiceCollection services, Func<IServiceProvider, string> connectionStringFactory)
    {
        services.AddDbContext<LoyaltyDbContext>((sp, options) => options.UseSqlite(connectionStringFactory(sp)));
        services.AddScoped<IPointsService, PointsService>();
        services.AddScoped<LoyaltyDataSeeder>();
        return services;
    }
}
