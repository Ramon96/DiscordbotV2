using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OldschoolRunescape;

public class OldschoolRunescapeActivityConfig : EntityConfig<OldschoolRunescapeActivity>
{
    public new void Configure(EntityTypeBuilder<OldschoolRunescapeActivity> builder)
    {
        base.Configure(builder);

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(activity => activity.Id)
            .IsUnique();

        builder.Property(activity => activity.Rank)
            .IsRequired();
        
        builder.Property(activity => activity.Score)
            .IsRequired();
        
        builder.Property(activity => activity.ActivityId)
            .IsRequired();
        
        builder.HasOne<OldschoolRunescapeUser>(activity => activity.User)
            .WithMany(user => user.Activities)
            .HasForeignKey(activity => activity.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}