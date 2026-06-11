using GLaDOS.Domain.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.Discord;

public class OsrsFuckupConfig : EntityConfig<OsrsFuckup>
{
    public override void Configure(EntityTypeBuilder<OsrsFuckup> builder)
    {
        base.Configure(builder);

        builder.HasKey(f => f.Id);

        builder.HasIndex(f => f.Id).IsUnique();

        builder.Property(f => f.FuckupDate).IsRequired();
        builder.Property(f => f.DiscordUserId).IsRequired();
    }
}
