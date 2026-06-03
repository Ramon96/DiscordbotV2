using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OldschoolRunescape;

public class OldschoolRunescapeStatsSnapshotConfig : EntityConfig<OldschoolRunescapeStatsSnapshot>
{
    public override void Configure(EntityTypeBuilder<OldschoolRunescapeStatsSnapshot> builder)
    {
        base.Configure(builder);

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.Id).IsUnique();

        builder.HasIndex(s => new { s.OldschoolRunescapeUserId, s.SnapshotDate });

        builder.Property(s => s.SnapshotDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.SkillId).IsRequired();
        builder.Property(s => s.Level).IsRequired();
        builder.Property(s => s.Experience).IsRequired();
        builder.Property(s => s.Rank).IsRequired();

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
