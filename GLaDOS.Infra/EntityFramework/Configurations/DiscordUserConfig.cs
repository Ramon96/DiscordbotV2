using GLaDOS.Domain.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations;

public class DiscordUserConfig : EntityConfig<DiscordUser>
{
    public override void Configure(EntityTypeBuilder<DiscordUser> builder)
    {
        base.Configure(builder);

        builder.ToTable("DiscordUsers");

        builder.Property(discordUser => discordUser.DiscordId)
            .HasColumnName("discord_id")
            .IsRequired();

        builder.HasIndex(discordUser => discordUser.DiscordId)
            .IsUnique();

        builder.HasMany(discordUser => discordUser.OldschoolRunescapeUsers)
            .WithOne(oldschoolRunescapeUser => oldschoolRunescapeUser.DiscordUser)
            .HasForeignKey(oldschoolRunescapeUser => oldschoolRunescapeUser.DiscordUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
