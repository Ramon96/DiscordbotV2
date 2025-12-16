using GLaDOS.Infra.EntityFramework;
using GLaDOS.ServiceCollection;
using GLaDOS.Scheduler.ServiceCollection;
using Microsoft.EntityFrameworkCore;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddCoreServices();

builder.Services.AddSchedulerServices(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

var backgroundJobs = app.Services.GetRequiredService<IBackgroundJobClient>();
backgroundJobs.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));
app.MapControllers();
app.MapHangfireDashboard();

app.Run();