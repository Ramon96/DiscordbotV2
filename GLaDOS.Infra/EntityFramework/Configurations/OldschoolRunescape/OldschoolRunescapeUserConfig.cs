using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OldschoolRunescape;

public class OldschoolRunescapeUserConfig : EntityConfig<OldschoolRunescapeUser>, IEntityTypeConfiguration<OldschoolRunescapeUser>
{
    public new void Configure(EntityTypeBuilder<OldschoolRunescapeUser> builder)
    {
        builder
            .Property(entity => entity.Username)
            .IsRequired();

        builder
            .HasIndex(entity => entity.Username)
            .IsUnique();

        builder.HasMany(user => user.Stats)
            .WithOne(stat => stat.User)
            .HasForeignKey(stat => stat.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.Bosses)
            .WithOne(boss => boss.User)
            .HasForeignKey(boss => boss.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}