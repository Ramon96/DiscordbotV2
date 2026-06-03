using GLaDOS.Domain.OsrsFlipping;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.OsrsFlipping;

public class OsrsItemMappingConfig : EntityConfig<OsrsItemMapping>
{
    public override void Configure(EntityTypeBuilder<OsrsItemMapping> builder)
    {
        base.Configure(builder);

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Id)
            .IsUnique();

        builder.HasIndex(e => e.OsrsItemId)
            .IsUnique();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.GeLimit)
            .IsRequired(false);
    }
}
