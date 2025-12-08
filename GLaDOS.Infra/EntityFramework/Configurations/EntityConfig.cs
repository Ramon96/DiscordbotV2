using GLaDOS.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations;

public abstract class EntityConfig<TEnity> where TEnity : Entity
{
    public void Configure(EntityTypeBuilder<TEnity> builder)
    {
        builder
            .Property(entity => entity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder
            .Property(entity => entity.ModifiedDate)
            .HasColumnType("timestamp without time zone")
            .HasColumnName("modified")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder
            .Property(entity => entity.CreatedDate)
            .HasColumnType("timestamp without time zone")
            .HasColumnName("created")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}