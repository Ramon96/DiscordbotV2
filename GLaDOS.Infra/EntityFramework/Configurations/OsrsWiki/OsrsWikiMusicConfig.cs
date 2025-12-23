using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Domain.OsrsWiki;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OsrsWiki;

public class OsrsWikiMusicConfig : EntityConfig<OsrsWikiMusic>
{
    public override void Configure(EntityTypeBuilder<OsrsWikiMusic> builder)
    {
        base.Configure(builder);
        
        builder.HasKey(stat => stat.Id);
        
        builder.HasIndex(stat => stat.Id)
            .IsUnique();
        
        builder.Property(music => music.Song)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(music => music.IsUnlocked)
            .IsRequired();

        builder.HasOne<OldschoolRunescapeUser>(music => music.User)
            .WithMany(user => user.Songs)
            .HasForeignKey(song => song.OldschoolRunescapeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}