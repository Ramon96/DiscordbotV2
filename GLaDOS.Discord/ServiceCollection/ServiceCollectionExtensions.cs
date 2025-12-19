using Discord;
using Discord.WebSocket;
using Glados.Discord.Commands;
using Glados.Discord.Contracts;
using Glados.Discord.Services;
using Glados.Discord.Services.Contracts;
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
            .AddSingleton<DiscordNotificationService>()
            .AddScoped<IDiscordUserService, DiscordUserService>()
            .AddSingleton<IDiscordCommand, AddDiscordUserCommand>()
            .AddSingleton<IDiscordCommand, ConnectOsrsUser>()
            .AddHostedService<CommandHandlerService>()
            .AddHostedService<HelloWorld>();

        return collection;
    }
}