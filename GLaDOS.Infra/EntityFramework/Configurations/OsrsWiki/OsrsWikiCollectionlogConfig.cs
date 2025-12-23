using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Domain.OsrsWiki;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OsrsWiki;

public class OsrsWikiCollectionlogConfig : EntityConfig<OsrsWikiCollectionLog>
{
    public override void Configure(EntityTypeBuilder<OsrsWikiCollectionLog> builder)
    {
        base.Configure(builder);
        
        builder.HasKey(stat => stat.Id);
        
        builder.HasIndex(stat => stat.Id)
            .IsUnique();

        builder.Property(collecitonLog => collecitonLog.ItemIds)
            .IsRequired()
            .HasColumnType("integer[]");
        
        builder.HasOne<OldschoolRunescapeUser>(collectionLog => collectionLog.User)
            .WithOne(oldschoolRunescapeUser => oldschoolRunescapeUser.CollectionLog)
            .HasForeignKey<OsrsWikiCollectionLog>(collectionLog => collectionLog.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}