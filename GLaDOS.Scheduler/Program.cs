using GLaDOS.Infra.EntityFramework;
using GLaDOS.Scheduler.Application.OldschoolRunescape;
using GLaDOS.Scheduler.Extensions;
using GLaDOS.Scheduler.ServiceCollection;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCoreServices(builder.Configuration);

builder.Services.AddTransient<HiscoreCalculator>();
builder.Services.AddTransient<HiscoreJob>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}


app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[]
        {
            new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
            {
                RequireSsl = false,
                SslRedirect = false,
                LoginCaseSensitive = true,
                Users = new[]
                {
                    new BasicAuthAuthorizationUser
                    {
                        Login = builder.Configuration["Hangfire:User"],
                        PasswordClear = builder.Configuration["Hangfire:Password"]
                    }
                }
            })
        }
    }
);
RecurringJob.AddOrUpdate<HiscoreJob>(
    "sync-hiscores",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Hourly);



app.Run();