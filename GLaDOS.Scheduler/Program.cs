using GLaDOS.Infra.EntityFramework;
using GLaDOS.Infra.Logging;
using GLaDOS.Scheduler.Application;
using GLaDOS.Scheduler.Application.Hangfire;
using GLaDOS.Scheduler.Application.OldschoolRunescape;
using GLaDOS.Scheduler.Application.OsrsFlipping;
using GLaDOS.Scheduler.Application.OsrsFlipping.Clients;
using GLaDOS.Scheduler.Application.Discord;
using GLaDOS.Scheduler.Application.Discord.Clients;
using GLaDOS.Scheduler.Application.Logging;
using GLaDOS.Scheduler.Application.Dashboard;
using GLaDOS.Scheduler.Application.Dashboard.Metrics;
using GLaDOS.Scheduler.Application.Swagger;
using GLaDOS.Scheduler.Extensions;
using GLaDOS.Scheduler.ServiceCollection;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Postgres(builder.Configuration.GetConnectionString("DefaultConnection")!));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDashboardAuth();
builder.Services.AddDashboardMetrics();

builder.Services.AddCoreServices(builder.Configuration);

// todo extension maken 
builder.Services.AddTransient<HiscoreCalculator>();
builder.Services.AddTransient<HiscoreJob>();
builder.Services.AddTransient<OsrsWikiSyncJob>();
builder.Services.AddTransient<OsrsPriceFetcherJob>();
builder.Services.AddTransient<OsrsItemMappingJob>();
builder.Services.AddTransient<StatsSnapshotJob>();
builder.Services.AddTransient<HottieOfTheDayJob>();
builder.Services.AddTransient<ShirtlessOldManJob>();
builder.Services.AddTransient<LogRetentionJob>();

builder.Services.AddHttpClient<IOsrsPriceClient, OsrsPriceClient>(client =>
{
    client.BaseAddress = new Uri("https://prices.runescape.wiki/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "MyDiscordBot - Market Analyzer Project");
});

builder.Services.AddHttpClient<IShirtlessOldManImageService, ShirtlessOldManImageService>(client =>
{
    client.BaseAddress = new Uri("https://api.flickr.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; GLaDOS/1.0)");
});


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.UseForwardedHeaders();

app.UseMiddleware<SwaggerBasicAuthMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[]
        {
            new HangfireAuthorizationFilter(
                builder.Configuration["Hangfire:Username"],
                builder.Configuration["Hangfire:Password"]
            )
        }
    }
);

// Serve the React dashboard SPA (built into wwwroot/dashboard) and let it own client-side routing.
app.MapGet("/dashboard", () => Results.Redirect("/dashboard/"));
app.MapFallbackToFile("dashboard/{*path:nonfile}", "dashboard/index.html");


// TODO: move job scheduling to a proper place (ex: HangfireJobScheduler.cs)
var runRecurringJobs = !app.Environment.IsDevelopment();

if (runRecurringJobs)
{
    RecurringJob.AddOrUpdate<HiscoreJob>(
        "sync-hiscores",
        job => job.ExecuteAsync(null,  CancellationToken.None),
        Cron.MinuteInterval(10));

    RecurringJob.AddOrUpdate<OsrsWikiSyncJob>(
        "sync-osrs-wiki",
        job => job.ExecuteAsync(null, CancellationToken.None),
        Cron.Hourly);

    RecurringJob.AddOrUpdate<OsrsPriceFetcherJob>(
        "fetch-osrs-prices",
        job => job.ExecuteAsync(null, CancellationToken.None),
        "*/5 * * * *");

    RecurringJob.AddOrUpdate<OsrsItemMappingJob>(
        "sync-item-mappings",
        job => job.ExecuteAsync(null, CancellationToken.None),
        Cron.Daily);

    RecurringJob.AddOrUpdate<StatsSnapshotJob>(
        "stats-snapshot",
        job => job.ExecuteAsync(null, CancellationToken.None),
        Cron.Daily);

    RecurringJob.AddOrUpdate<HottieOfTheDayJob>(
        "hottie-of-the-day",
        job => job.ExecuteAsync(null, CancellationToken.None),
        Cron.Daily);

    RecurringJob.AddOrUpdate<ShirtlessOldManJob>(
        "shirtless-old-man",
        job => job.ExecuteAsync(null, CancellationToken.None),
        "0 10 * * 1");

    RecurringJob.AddOrUpdate<LogRetentionJob>(
        "log-retention",
        job => job.ExecuteAsync(null, CancellationToken.None),
        Cron.Daily);

    BackgroundJob.Enqueue<OsrsItemMappingJob>(job => job.ExecuteAsync(null, CancellationToken.None));
}
else
{
    RecurringJob.RemoveIfExists("sync-hiscores");
    RecurringJob.RemoveIfExists("sync-osrs-wiki");
    RecurringJob.RemoveIfExists("fetch-osrs-prices");
    RecurringJob.RemoveIfExists("sync-item-mappings");
    RecurringJob.RemoveIfExists("stats-snapshot");
    RecurringJob.RemoveIfExists("hottie-of-the-day");
    RecurringJob.RemoveIfExists("shirtless-old-man");
    RecurringJob.RemoveIfExists("log-retention");


    app.MapPost("/jobs/hiscore/trigger", (HttpContext _) =>
        {
            var id = BackgroundJob.Enqueue<HiscoreJob>(job => job.ExecuteAsync(null, CancellationToken.None));
            return Results.Accepted($"/hangfire/jobs/details/{id}");
        })
        .WithTags("Jobs");

    app.MapPost("/jobs/osrswiki/trigger", (HttpContext _) =>
        {
            var id = BackgroundJob.Enqueue<OsrsWikiSyncJob>(job => job.ExecuteAsync(null, CancellationToken.None));
            return Results.Accepted($"/hangfire/jobs/details/{id}");
        })
        .WithTags("Jobs");

    app.MapPost("/jobs/osrsprices/trigger", (HttpContext _) =>
        {
            var id = BackgroundJob.Enqueue<OsrsPriceFetcherJob>(job => job.ExecuteAsync(null, CancellationToken.None));
            return Results.Accepted($"/hangfire/jobs/details/{id}");
        })
        .WithTags("Jobs");

    app.MapPost("/jobs/itemmappings/trigger", (HttpContext _) =>
        {
            var id = BackgroundJob.Enqueue<OsrsItemMappingJob>(job => job.ExecuteAsync(null, CancellationToken.None));
            return Results.Accepted($"/hangfire/jobs/details/{id}");
        })
        .WithTags("Jobs");
}

app.Run();