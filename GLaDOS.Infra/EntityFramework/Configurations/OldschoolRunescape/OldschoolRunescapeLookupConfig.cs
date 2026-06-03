using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OldschoolRunescape;

public class OldschoolRunescapeLookupConfig : EntityConfig<OldschoolRunescapeLookup>
{
    public override void Configure(EntityTypeBuilder<OldschoolRunescapeLookup> builder)
    {
        base.Configure(builder);

        builder.HasKey(l => l.Id);

        builder.HasIndex(l => l.Id).IsUnique();

        builder.HasIndex(l => new { l.OldschoolRunescapeUserId, l.DiscordUserId });

        builder.Property(l => l.DiscordUserId).IsRequired();

        builder.Property(l => l.LookupDate)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
