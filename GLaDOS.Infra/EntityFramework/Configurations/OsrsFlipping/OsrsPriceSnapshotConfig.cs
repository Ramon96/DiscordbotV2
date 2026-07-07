using GLaDOS.Domain.OsrsFlipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OsrsFlipping;

public class OsrsPriceSnapshotConfig : EntityConfig<OsrsPriceSnapshot>
{
    public override void Configure(EntityTypeBuilder<OsrsPriceSnapshot> builder)
    {
        base.Configure(builder);

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Id)
            .IsUnique();

        builder.HasIndex(e => new { e.OsrsItemId, e.Timestamp });

        // Supports "latest snapshot" lookups (ORDER BY Timestamp DESC) on the dashboard, which the
        // composite index above can't serve because it leads with OsrsItemId.
        builder.HasIndex(e => e.Timestamp);

        builder.Property(e => e.AvgBuyPrice)
            .IsRequired();

        builder.Property(e => e.AvgSellPrice)
            .IsRequired();

        builder.Property(e => e.Volume)
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
