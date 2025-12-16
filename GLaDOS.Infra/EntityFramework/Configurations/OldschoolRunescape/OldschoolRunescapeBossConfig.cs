using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OldschoolRunescape;

public class OldschoolRunescapeBossConfig : IEntityTypeConfiguration<OldschoolRunescapeBoss>
{
    public void Configure(EntityTypeBuilder<OldschoolRunescapeBoss> builder)
    {
        builder.HasKey(boss => boss.Id);

        builder.Property(boss => boss.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(boss => boss.Id)
            .IsUnique();
    }
}