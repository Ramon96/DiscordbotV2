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
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.MessageContent
        };

        var collection = services
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>(sp => new DiscordSocketClient(sp.GetRequiredService<DiscordSocketConfig>()))
            .AddSingleton<IDiscordClient, DiscordClient>()
            .AddSingleton<DiscordNotificationService>()
            .AddScoped<IDiscordUserService, DiscordUserService>()
            .AddSingleton<AIService>()
            .AddSingleton<GitHubService>()
            .AddSingleton<FeatureGuardService>()
            .AddHostedService<CommandHandlerService>()
            .AddHostedService<DiscordClient>()
            .AddHostedService<SassyReplyService>();

        var commandType = typeof(IDiscordCommand);
        var commandTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && commandType.IsAssignableFrom(t))
            .ToList();

        Console.WriteLine($"Auto-discovering IDiscordCommand implementations...");
        foreach (var type in commandTypes)
        {
            Console.WriteLine($"  Registered: {type.Name}");
            services.AddSingleton(typeof(IDiscordCommand), type);
        }
        Console.WriteLine($"Discovered {commandTypes.Count} command(s).");

        return collection;
    }
}
