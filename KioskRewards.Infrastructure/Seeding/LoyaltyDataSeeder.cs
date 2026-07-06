using KioskRewards.Domain.Entities;
using KioskRewards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KioskRewards.Infrastructure.Seeding;

/// <summary>
/// Seeds a couple of demo accounts so there's something to look at before real Members exist.
/// Skips itself if any accounts are already there. TEMP - uses fixed demo keys for now.
/// </summary>
public sealed class LoyaltyDataSeeder
{
    // fixed guids so the demo data stays predictable between runs
    public static readonly Guid DemoMemberAlpha = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000001");
    public static readonly Guid DemoMemberBeta = Guid.Parse("b2b2b2b2-0000-0000-0000-000000000002");

    private readonly LoyaltyDbContext _db;

    public LoyaltyDataSeeder(LoyaltyDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct);   // make sure the schema's actually there first

        if (!await _db.PointsAccounts.AnyAsync(ct))
        {
            var now = DateTime.UtcNow;

            var alpha = PointsAccount.Create(DemoMemberAlpha);
            alpha.Earn(500, "Welcome bonus", now);
            alpha.Earn(250, "Sale #1001", now);

            var beta = PointsAccount.Create(DemoMemberBeta);
            beta.Earn(120, "Welcome bonus", now);

            await _db.PointsAccounts.AddRangeAsync(new[] { alpha, beta }, ct);
            await _db.SaveChangesAsync(ct);
        }

        // No OrderCode seeding here anymore - the order-code catalog now lives as Umbraco content
        // ("CompanyOrder" nodes), not EF rows. OrderCode rows only get created when a code is
        // actually claimed (see OrderClaimService) - see docs/SEED-DATA.md for the demo codes.
    }
}
