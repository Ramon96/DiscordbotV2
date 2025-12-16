using Hangfire;
using Hangfire.PostgreSql;

namespace GLaDOS.Scheduler.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var collection = services
            .AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);

                }))
                .AddHangfireServer();

        return collection;
    }
}