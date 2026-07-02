using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KioskRewards.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` build the context without spinning up all of Umbraco.
/// The connection string here is just for the EF tooling - the real app uses its own.
/// </summary>
public sealed class LoyaltyDbContextFactory : IDesignTimeDbContextFactory<LoyaltyDbContext>
{
    public LoyaltyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LoyaltyDbContext>()
            .UseSqlite("Data Source=loyalty.design.db")
            .Options;

        return new LoyaltyDbContext(options);
    }
}
