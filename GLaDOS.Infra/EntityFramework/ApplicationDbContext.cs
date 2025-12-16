using GLaDOS.Domain.Discord;
using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Infra.EntityFramework;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<DiscordUser> DiscordUsers { get; set; }
    public DbSet<OldschoolRunescapeUser> OldschoolRunescapeUsers { get; set; }
    public DbSet<OldschoolRunescapeStat> OldschoolRunescapeStats { get; set; }
    public DbSet<OldschoolRunescapeBoss> OldschoolRunescapeBosses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
//  dotnet ef migrations add InitialCreate --project GLaDOS.Infra --startup-project GLaDOS.Scheduler