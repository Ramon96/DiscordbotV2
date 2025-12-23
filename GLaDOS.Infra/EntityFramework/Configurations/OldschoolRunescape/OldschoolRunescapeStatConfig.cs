using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OldschoolRunescape;

public class OldschoolRunescapeStatConfig : EntityConfig<OldschoolRunescapeStat>
{
    public override void Configure(EntityTypeBuilder<OldschoolRunescapeStat> builder)
    {
        base.Configure(builder);

        builder.HasKey(stat => stat.Id);
        
        builder.HasIndex(stat => stat.Id)
            .IsUnique();
        
        builder.Property(stat => stat.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(stat => stat.Level)
            .IsRequired();

        builder.Property(stat => stat.Experience)
            .IsRequired();

        builder.Property(stat => stat.Rank)
            .IsRequired();
        
        builder.Property(stat => stat.SkillId)
            .IsRequired();

        builder.HasOne<OldschoolRunescapeUser>(stat => stat.User)
            .WithMany(user => user.Stats)
            .HasForeignKey(stat => stat.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}