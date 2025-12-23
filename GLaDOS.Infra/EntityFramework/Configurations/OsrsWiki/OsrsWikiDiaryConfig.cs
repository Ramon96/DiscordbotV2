using System.Text.Json;
using System.Text.Json.Serialization;
using GLaDOS.Domain.OsrsWiki;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OsrsWiki;

public class OsrsWikiDiaryConfig : EntityConfig<OsrsWikiDiary>
{
    public override void Configure(EntityTypeBuilder<OsrsWikiDiary> builder)
    {
        base.Configure(builder);
        
        builder.HasKey(stat => stat.Id);
        
        builder.HasIndex(stat => stat.Id)
            .IsUnique();
        
        builder.Property(diary => diary.Region)
            .IsRequired()
            .HasMaxLength(50);
        
        ConfigureTier(builder.OwnsOne(tier => tier.Easy), "easy");
        ConfigureTier(builder.OwnsOne(tier => tier.Medium), "medium");
        ConfigureTier(builder.OwnsOne(tier => tier.Hard), "hard");
        ConfigureTier(builder.OwnsOne(tier => tier.Elite), "elite");
    }
    
    private void ConfigureTier(OwnedNavigationBuilder<OsrsWikiDiary, DiaryTier> tierBuilder, string prefix)
    {
        tierBuilder.Property(tier => tier.IsComplete)
            .HasColumnName($"{prefix}_complete")
            .HasDefaultValue(false);
        
        tierBuilder.Property(tier => tier.Tasks)
            .HasColumnName($"{prefix}_tasks")
            .HasColumnType("jsonb");             
    }
}