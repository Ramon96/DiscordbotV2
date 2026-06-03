using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OldschoolRunescape;

public class OldschoolRunescapeActivitySnapshotConfig : EntityConfig<OldschoolRunescapeActivitySnapshot>
{
    public override void Configure(EntityTypeBuilder<OldschoolRunescapeActivitySnapshot> builder)
    {
        base.Configure(builder);

        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.Id).IsUnique();

        builder.HasIndex(a => new { a.OldschoolRunescapeUserId, a.SnapshotDate });

        builder.Property(a => a.SnapshotDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.ActivityId).IsRequired();
        builder.Property(a => a.Score).IsRequired();
        builder.Property(a => a.Rank).IsRequired();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
