using KioskRewards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KioskRewards.Infrastructure.Persistence.Configurations;

internal sealed class OrderCodeConfiguration : IEntityTypeConfiguration<OrderCode>
{
    public void Configure(EntityTypeBuilder<OrderCode> builder)
    {
        builder.ToTable("OrderCodes");

        builder.HasKey(o => o.Id);   // auto-increment id, nothing fancy

        builder.Property(o => o.ContentKey).IsRequired();
        builder.HasIndex(o => o.ContentKey).IsUnique();   // the actual "no double-claiming" guarantee lives here

        builder.Property(o => o.ClaimedByMemberKey).IsRequired();
        builder.Property(o => o.ClaimedUtc).IsRequired();
    }
}
