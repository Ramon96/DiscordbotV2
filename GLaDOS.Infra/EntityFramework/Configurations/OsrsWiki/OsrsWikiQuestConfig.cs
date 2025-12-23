using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Domain.OsrsWiki;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OsrsWiki;

public class OsrsWikiQuestConfig : EntityConfig<OsrsWikiQuest>
{
    public override void Configure(EntityTypeBuilder<OsrsWikiQuest> builder)
    {
        base.Configure(builder);
        
        builder.HasKey(stat => stat.Id);
        
        builder.HasIndex(stat => stat.Id)
            .IsUnique();
        
        builder.Property(quest => quest.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(quest => quest.Status)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(quest => quest.Status)
            .IsRequired()
            .HasMaxLength(20) 
            .HasConversion<string>();
        
        builder.HasOne<OldschoolRunescapeUser>(quest => quest.User)
            .WithMany(user => user.Quests)
            .HasForeignKey(quest => quest.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}