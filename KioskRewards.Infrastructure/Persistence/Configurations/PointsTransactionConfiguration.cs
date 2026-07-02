using KioskRewards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KioskRewards.Infrastructure.Persistence.Configurations;

internal sealed class PointsTransactionConfiguration : IEntityTypeConfiguration<PointsTransaction>
{
    public void Configure(EntityTypeBuilder<PointsTransaction> builder)
    {
        builder.ToTable("PointsTransactions");

        builder.HasKey(t => t.Id);   // auto-increment id, nothing fancy

        builder.Property(t => t.Amount).IsRequired();
        builder.Property(t => t.Type).IsRequired().HasConversion<int>();
        builder.Property(t => t.Description).IsRequired().HasMaxLength(200);
        builder.Property(t => t.CreatedUtc).IsRequired();

        // we always query history per member ordered by date, so index for that
        builder.HasIndex(t => new { t.MemberKey, t.CreatedUtc });
    }
}
