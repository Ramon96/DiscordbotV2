using Glados.Discord.ServiceCollection;
using GLaDOS.OldschoolRunescape.ServiceCollection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GLaDOS.Scheduler.ServiceCollection;

public static class ServiceCollectionExtensions
{

    public static IHttpClientBuilder AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        var collection = services
            .AddSchedulerServices(configuration)
            .AddDiscordServices()
            .AddOldschoolRunescapeServices();

        return collection;
    }
}