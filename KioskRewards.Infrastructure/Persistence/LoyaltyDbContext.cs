using KioskRewards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KioskRewards.Infrastructure.Persistence;

/// <summary>
/// EF context for our own loyalty tables - totally separate from Umbraco's own database.
/// Doubles as both the repository and unit of work here.
/// </summary>
public class LoyaltyDbContext : DbContext
{
    public LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options) : base(options) { }

    public DbSet<PointsAccount> PointsAccounts => Set<PointsAccount>();
    public DbSet<PointsTransaction> PointsTransactions => Set<PointsTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LoyaltyDbContext).Assembly);

        // SQLite has no real rowversion type so we skip it in dev; SQL Server gets the real thing
        if (Database.IsSqlite())
            modelBuilder.Entity<PointsAccount>().Ignore(a => a.RowVersion);
        else
            modelBuilder.Entity<PointsAccount>().Property(a => a.RowVersion).IsRowVersion();
    }
}
