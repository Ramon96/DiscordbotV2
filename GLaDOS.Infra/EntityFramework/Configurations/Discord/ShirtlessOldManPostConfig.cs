using GLaDOS.Domain.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.Discord;

public class ShirtlessOldManPostConfig : EntityConfig<ShirtlessOldManPost>
{
    public override void Configure(EntityTypeBuilder<ShirtlessOldManPost> builder)
    {
        base.Configure(builder);

        builder.HasKey(post => post.Id);

        builder.HasIndex(post => post.Id).IsUnique();

        // One row per Discord message; de-duplicates the backfill and any re-runs.
        builder.HasIndex(post => post.MessageId).IsUnique();

        builder.Property(post => post.ImageUrl).IsRequired();

        builder.Property(post => post.PostedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
