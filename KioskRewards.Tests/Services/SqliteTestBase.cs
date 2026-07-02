using KioskRewards.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KioskRewards.Tests.Services;

/// <summary>
/// Spins up a real in-memory SQLite db per test. Using actual SQLite instead of the EF in-memory
/// provider means we're testing real relational behaviour (FKs, keys), not just EF's fake version of it.
/// </summary>
public abstract class SqliteTestBase : IDisposable
{
    private readonly SqliteConnection _connection;

    protected SqliteTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        Db = NewContext();
        Db.Database.EnsureCreated();
    }

    /// The context this test actually uses
    protected LoyaltyDbContext Db { get; }

    /// A new context on the same db, like a fresh request/DI scope would get
    protected LoyaltyDbContext NewContext()
        => new(new DbContextOptionsBuilder<LoyaltyDbContext>().UseSqlite(_connection).Options);

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
