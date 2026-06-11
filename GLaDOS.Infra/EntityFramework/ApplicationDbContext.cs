using GLaDOS.Domain.Discord;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Domain.OsrsFlipping;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Infra.EntityFramework;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<DiscordUser> DiscordUsers { get; set; }
    public DbSet<HottieOfTheDay> HottieOfTheDays { get; set; }
    public DbSet<OldschoolRunescapeUser> OldschoolRunescapeUsers { get; set; }
    public DbSet<OldschoolRunescapeStat> OldschoolRunescapeStats { get; set; }
    public DbSet<OldschoolRunescapeActivity> OldschoolRunescapeActivities { get; set; }
    public DbSet<OsrsItemMapping> OsrsItemMappings { get; set; }
    public DbSet<OsrsPriceSnapshot> OsrsPriceSnapshots { get; set; }
    public DbSet<OldschoolRunescapeStatsSnapshot> OldschoolRunescapeStatsSnapshots { get; set; }
    public DbSet<OldschoolRunescapeActivitySnapshot> OldschoolRunescapeActivitySnapshots { get; set; }
    public DbSet<OldschoolRunescapeLookup> OldschoolRunescapeLookups { get; set; }
    public DbSet<OsrsFuckup> OsrsFuckups { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);


    }
}
