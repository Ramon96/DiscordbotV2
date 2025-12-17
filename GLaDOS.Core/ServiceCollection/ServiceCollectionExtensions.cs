using Microsoft.Extensions.DependencyInjection;
using Glados.Discord.ServiceCollection;
using GLaDOS.OldschoolRunescape.ServiceCollection;

namespace GLaDOS.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddCoreServices(this IServiceCollection services)
    {
        var collection = services
            .AddDiscordServices()
            .AddOldschoolRunescapeServices();

        return collection;
    }
}