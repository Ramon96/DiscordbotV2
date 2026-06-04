using Discord;
using Discord.WebSocket;
using Glados.Discord.AI;
using Glados.Discord.Commands;
using Glados.Discord.Services;
using Glados.Discord.Services.Contracts;
using IDiscordClient = Glados.Discord.Contracts.IDiscordClient;
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
            .AddSingleton<IDiscordClient, DiscordClient>()
            .AddSingleton<DiscordNotificationService>()
            .AddScoped<IDiscordUserService, DiscordUserService>()
            .AddSingleton<AIService>()
            .AddSingleton<GitHubService>()
            .AddSingleton<FeatureGuardService>()
            .AddSingleton<IDiscordCommand, AddDiscordUserCommand>()
            .AddSingleton<IDiscordCommand, ConnectOsrsUser>()
            .AddSingleton<IDiscordCommand, NameChangeOsrsUser>()
            .AddSingleton<IDiscordCommand, OsrsFlipsCommand>()
            .AddSingleton<IDiscordCommand, LookupCommand>()
            .AddSingleton<IDiscordCommand, FeatureCommand>()
            .AddHostedService<CommandHandlerService>()
            .AddHostedService<DiscordClient>();

        return collection;
    }
}