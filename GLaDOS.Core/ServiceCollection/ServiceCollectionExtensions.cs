using Microsoft.Extensions.DependencyInjection;
using Glados.Discord.ServiceCollection;

namespace GLaDOS.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        var collection = services
            .AddDiscordServices();

        return collection;
    }
}