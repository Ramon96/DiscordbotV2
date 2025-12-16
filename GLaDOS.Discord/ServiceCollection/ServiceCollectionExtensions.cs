using Discord;
using Discord.WebSocket;
using Glados.Discord.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordServices(this IServiceCollection services)
    {
        var config = new DiscordConfig()
        {
            
        };

        var collection = services
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<IHelloWorld, HelloWorld>()
            .AddHostedService<HelloWorld>();

        return collection;
    }
}