using KioskRewards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KioskRewards.Infrastructure.Persistence.Configurations;

internal sealed class PointsAccountConfiguration : IEntityTypeConfiguration<PointsAccount>
{
    public void Configure(EntityTypeBuilder<PointsAccount> builder)
    {
        builder.ToTable("PointsAccounts");

        // one account per member, the key comes from the Member itself, never auto-generated
        builder.HasKey(a => a.MemberKey);
        builder.Property(a => a.MemberKey).ValueGeneratedNever();

        builder.Property(a => a.Balance).IsRequired();

        // Transactions is read-only from outside the aggregate, so tell EF to use the _transactions
        // field directly instead of going through the property.
        builder.HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.MemberKey)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // RowVersion itself gets configured over in LoyaltyDbContext.OnModelCreating instead,
        // since it depends on which provider we're running on.
    }
}
