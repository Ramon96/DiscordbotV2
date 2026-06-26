using System.Text.RegularExpressions;
using Discord;
using Glados.Discord.ServiceCollection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Glados.Discord.Tests;

/// <summary>
/// Contract / smoke tests that run against every IDiscordCommand the bot auto-discovers.
///
/// These exist because user-submitted features (added via the /feature command) are
/// auto-discovered and bulk-registered with Discord at startup. A single malformed command
/// definition throws on SlashCommandBuilder.Build() and breaks command registration for the
/// ENTIRE bot, not just that feature. These tests catch that class of breakage on the PR,
/// before it is merged.
///
/// The tests are fully data-driven: they discover commands the same way production does, so a
/// new feature is covered automatically with no test changes.
/// </summary>
public class CommandContractTests
{
    // Discord's slash command name rule: lowercase letters, digits, underscore and dash, 1-32 chars.
    // See https://discord.com/developers/docs/interactions/application-commands#application-command-object
    private static readonly Regex NamePattern = new("^[\\w-]{1,32}$", RegexOptions.Compiled);

    // Commands are resolved once, exactly as the bot resolves them in production
    // (real AddDiscordServices registration + reflection-based discovery).
    private static readonly IReadOnlyList<IDiscordCommand> Commands = DiscoverCommands();

    private static IReadOnlyList<IDiscordCommand> DiscoverCommands()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var provider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddDiscordServices()
            .BuildServiceProvider();

        return provider.GetServices<IDiscordCommand>().ToList();
    }

    public static IEnumerable<object[]> DiscoveredCommands()
    {
        for (var i = 0; i < Commands.Count; i++)
        {
            // Index + display name keep the theory cases serializable and readable in test output.
            yield return new object[] { i, Commands[i].GetType().Name };
        }
    }

    [Fact]
    public void At_least_one_command_is_discovered()
    {
        Assert.NotEmpty(Commands);
    }

    [Theory]
    [MemberData(nameof(DiscoveredCommands))]
    public void Command_name_is_valid(int index, string typeName)
    {
        var command = Commands[index];
        var name = command.Name;

        Assert.False(string.IsNullOrWhiteSpace(name), $"{typeName}.Name is null or empty.");
        Assert.True(name == name.ToLowerInvariant(), $"{typeName}.Name '{name}' must be all lowercase.");
        Assert.True(NamePattern.IsMatch(name),
            $"{typeName}.Name '{name}' must match Discord's slash command rule ^[\\w-]{{1,32}}$ " +
            "(lowercase letters, digits, underscore or dash; no spaces; 1-32 chars).");
    }

    [Theory]
    [MemberData(nameof(DiscoveredCommands))]
    public void Command_definition_builds_and_matches_name(int index, string typeName)
    {
        var command = Commands[index];

        SlashCommandProperties definition;
        try
        {
            definition = command.GetCommandDefinition();
        }
        catch (Exception ex)
        {
            // This is the key check: Discord.Net validates the whole definition on .Build().
            // A throw here is exactly what would crash command registration at bot startup.
            Assert.Fail($"{typeName}.GetCommandDefinition() threw {ex.GetType().Name}: {ex.Message}");
            throw; // unreachable, keeps the compiler happy about 'definition' assignment
        }

        Assert.NotNull(definition);
        Assert.True(definition.Name.IsSpecified, $"{typeName} definition has no name set.");
        Assert.Equal(command.Name, definition.Name.Value);
    }

    [Fact]
    public void Command_names_are_unique()
    {
        var duplicates = Commands
            .GroupBy(c => c.Name)
            .Where(g => g.Count() > 1)
            .Select(g => $"'{g.Key}' -> {string.Join(", ", g.Select(c => c.GetType().Name))}")
            .ToList();

        Assert.True(duplicates.Count == 0,
            "Duplicate command names break registration. Conflicts: " + string.Join("; ", duplicates));
    }
}
