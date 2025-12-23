using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Domain.OsrsWiki;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OldschoolRunescape;

public class OldschoolRunescapeUserConfig : EntityConfig<OldschoolRunescapeUser>, IEntityTypeConfiguration<OldschoolRunescapeUser>
{
    public new void Configure(EntityTypeBuilder<OldschoolRunescapeUser> builder)
    {
        base.Configure(builder);

        builder
            .Property(entity => entity.Username)
            .IsRequired();

        builder
            .HasIndex(entity => entity.Username)
            .IsUnique();

        builder.HasMany(user => user.Stats)
            .WithOne(stat => stat.User)
            .HasForeignKey(stat => stat.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.Activities)
            .WithOne(activity => activity.User)
            .HasForeignKey(activity => activity.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(user => user.Songs)
            .WithOne(song => song.User)
            .HasForeignKey(song => song.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(user => user.Quests)
            .WithOne(quest => quest.User)
            .HasForeignKey(quest => quest.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(user => user.Diaries)
            .WithOne(diary => diary.User)
            .HasForeignKey(quest => quest.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(user => user.CollectionLog)
            .WithOne(collectionLog => collectionLog.User)
            .HasForeignKey<OsrsWikiCollectionLog>(collectionLog => collectionLog.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}