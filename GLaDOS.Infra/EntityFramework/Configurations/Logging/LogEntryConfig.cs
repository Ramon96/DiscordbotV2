using GLaDOS.Infra.EntityFramework.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLaDOS.Infra.EntityFramework.Configurations.Logging;

public class LogEntryConfig : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        // The logs table is created and owned by the Serilog sink, so keep it out of EF
        // migrations while still mapping it for LINQ queries.
        builder.ToTable("logs", table => table.ExcludeFromMigrations());

        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).HasColumnName("id");
        builder.Property(log => log.Timestamp).HasColumnName("timestamp");
        builder.Property(log => log.Level).HasColumnName("level");
        builder.Property(log => log.Message).HasColumnName("message");
        builder.Property(log => log.Exception).HasColumnName("exception");
        builder.Property(log => log.SourceContext).HasColumnName("source_context");
        builder.Property(log => log.Properties).HasColumnName("properties").HasColumnType("jsonb");
    }
}
