using GLaDOS.Domain.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.Discord;

public class HottieOfTheDayConfig : EntityConfig<HottieOfTheDay>
{
    public override void Configure(EntityTypeBuilder<HottieOfTheDay> builder)
    {
        base.Configure(builder);

        builder.HasKey(h => h.Id);

        builder.HasIndex(h => h.Id).IsUnique();

        builder.HasIndex(h => new { h.DiscordUserId, h.DateAwarded }).IsUnique();

        builder.Property(h => h.DateAwarded).IsRequired();
    }
}
