using GLaDOS.Domain.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.Discord;

public class DiscordUserConfig : EntityConfig<DiscordUser>, IEntityTypeConfiguration<DiscordUser>
{
    public new void Configure(EntityTypeBuilder<DiscordUser> builder)
    {
        base.Configure(builder);
        
        builder
            .Property(entity => entity.DiscordId)
            .IsRequired();
        
        builder
            .HasIndex(entity => entity.DiscordId)
            .IsUnique();
    }
}