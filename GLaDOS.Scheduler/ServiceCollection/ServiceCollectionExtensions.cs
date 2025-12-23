using Glados.Discord.ServiceCollection;
using GLaDOS.Infra.ServiceCollection;
using GLaDOS.OldschoolRunescape.ServiceCollection;
using GLaDOS.OsrsWiki.ServiceCollection;


namespace GLaDOS.Scheduler.ServiceCollection;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        var collection = services
            .AddInfraServices()
            .AddSchedulerServices(configuration)
            .AddDiscordServices()
            .AddOldschoolRunescapeServices()
            .AddOsrsWikiServices();

        return collection;
    }
}