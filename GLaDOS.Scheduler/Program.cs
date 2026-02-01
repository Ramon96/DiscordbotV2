using GLaDOS.Infra.EntityFramework;
using GLaDOS.Scheduler.Application;
using GLaDOS.Scheduler.Application.Hangfire;
using GLaDOS.Scheduler.Application.OldschoolRunescape;
using GLaDOS.Scheduler.Application.Swagger;
using GLaDOS.Scheduler.Extensions;
using GLaDOS.Scheduler.ServiceCollection;
using Microsoft.EntityFrameworkCore;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCoreServices(builder.Configuration);

// todo extension maken 
builder.Services.AddTransient<HiscoreCalculator>();
builder.Services.AddTransient<HiscoreJob>();
builder.Services.AddTransient<OsrsWikiSyncJob>();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.UseMiddleware<SwaggerBasicAuthMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

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
}
else
{
    RecurringJob.RemoveIfExists("sync-hiscores");
    RecurringJob.RemoveIfExists("sync-osrs-wiki");


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
}

app.Run();