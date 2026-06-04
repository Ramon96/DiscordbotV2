using System.Reflection;
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
            .AddHostedService<CommandHandlerService>()
            .AddHostedService<DiscordClient>();

        var commandType = typeof(IDiscordCommand);
        var commandTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && commandType.IsAssignableFrom(t));

        foreach (var type in commandTypes)
        {
            Console.WriteLine($"Auto-registered command: {type.Name}");
            services.AddSingleton(typeof(IDiscordCommand), type);
        }

        return collection;
    }
}
