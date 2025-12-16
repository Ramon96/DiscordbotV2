using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OldschoolRunescape;

public class OldschoolRunescapeStatConfig : IEntityTypeConfiguration<OldschoolRunescapeStat>
{
    public void Configure(EntityTypeBuilder<OldschoolRunescapeStat> builder)
    {
        builder.HasKey(stat => stat.Id);

        builder.Property(stat => stat.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(stat => stat.Level)
            .IsRequired();

        builder.Property(stat => stat.Experience)
            .IsRequired();

        builder.Property(stat => stat.Rank)
            .IsRequired();

        builder.HasOne<OldschoolRunescapeUser>()
            .WithMany(user => user.Stats)
            .HasForeignKey(stat => stat.RunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}