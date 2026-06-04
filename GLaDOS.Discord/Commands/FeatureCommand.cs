using System.Diagnostics;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Glados.Discord.Services;
using Microsoft.Extensions.Configuration;

namespace Glados.Discord.Commands;

public partial class FeatureCommand : IDiscordCommand
{
    private readonly FeatureGuardService _guardService;
    private readonly IConfiguration _configuration;

    private static readonly TimeSpan OpencodeTimeout = TimeSpan.FromMinutes(10);

    public FeatureCommand(FeatureGuardService guardService, IConfiguration configuration)
    {
        _guardService = guardService;
        _configuration = configuration;
    }

    public string Name => "feature";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Request a feature. The bot uses opencode to analyze, build, and submit a pull request.")
            .AddOption("description", ApplicationCommandOptionType.String, "Describe the feature you want to add to the bot", isRequired: true)
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken ct = default)
    {
        await command.DeferAsync();

        var description = command.Data.Options.FirstOrDefault(o => o.Name == "description")?.Value as string;
        if (string.IsNullOrWhiteSpace(description))
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = "Please provide a description of the feature you want.");
            return;
        }

        var (inputOk, inputReason) = _guardService.ValidateInput(description);
        if (!inputOk)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"Feature request blocked. {inputReason}");
            return;
        }

        await command.ModifyOriginalResponseAsync(props =>
            props.Content = $"Working on your feature... (this may take a few minutes)\n\n> {description}");

        var prompt = BuildPrompt(description);

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "opencode",
                    ArgumentList = { "run", prompt, "--dir", "/repo", "--dangerously-skip-permissions" },
                    WorkingDirectory = "/repo",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            var completed = process.WaitForExit((int)OpencodeTimeout.TotalMilliseconds);
            if (!completed)
            {
                process.Kill(entireProcessTree: true);
                await command.ModifyOriginalResponseAsync(props =>
                    props.Content = "Timed out after 10 minutes. The feature might be too complex — try breaking it into smaller pieces.");
                return;
            }

            var output = await outputTask;
            var error = await errorTask;

            Console.WriteLine($"[Feature] opencode exit={process.ExitCode}");

            if (process.ExitCode != 0)
            {
                var errPreview = error.Length > 800 ? error[..800] + "..." : error;
                await command.ModifyOriginalResponseAsync(props =>
                    props.Content = $"opencode failed (exit {process.ExitCode}):\n```\n{errPreview}\n```");
                return;
            }

            var prUrl = ExtractPrUrl(output + error);
            if (prUrl is not null)
            {
                await command.ModifyOriginalResponseAsync(props =>
                    props.Content = $"Pull request created: {prUrl}");
            }
            else
            {
                var preview = output.Length > 1500 ? output[..1500] + "..." : output;
                await command.ModifyOriginalResponseAsync(props =>
                    props.Content = $"Done, but couldn't find a PR URL in the output:\n```\n{preview}\n```");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Feature] Process failed: {ex}");
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"Failed to run opencode: {ex.Message}");
        }
    }

    private static string BuildPrompt(string description)
    {
        return $"""
            IMMUTABLE SAFETY RULES — violating any of these is FORBIDDEN:
            1. NEVER delete or modify: Program.cs, ServiceCollectionExtensions.cs, any .csproj file, docker-compose.yml, Dockerfile, .env, appsettings*.json, anything in .github/
            2. NEVER run destructive shell commands: rm -rf, rm -r, del, format, shred, dd, mkfs, > /dev/sda
            3. NEVER download or install external software: no apt-get, pip, npm install, curl, wget for executables
            4. NEVER modify database schema: no EF migrations, no DbContext changes, no ALTER/CREATE/DROP TABLE
            5. NEVER create network listeners, reverse shells, cron jobs, or daemons
            6. NEVER hardcode secrets, API keys, tokens, or connection strings
            7. NEVER modify .gitignore or .git/config
            8. You operate inside a Docker container — you CANNOT access the host VPS. Only the /repo directory is available.

            ALLOWED ACTIONS:
            - Create new C# files in GLaDOS.Discord/Commands/ or GLaDOS.Discord/Services/
            - Run dotnet build to verify compilation (use bash)
            - Create git branches, commit, push, and open PRs (use bash)
            - Read any existing files to understand the codebase

            WORKFLOW:
            1. Read relevant existing code to understand patterns and conventions
            2. Create a new git branch with a descriptive name based on the feature
            3. Write the new code files
            4. Run `dotnet build` to verify the code compiles
            5. Commit with a descriptive message, push the branch
            6. Create a pull request to main with a clear title and summary

            FEATURE REQUEST:
            {description}
            """;
    }

    [GeneratedRegex(@"https://github\.com/[^/\s]+/[^/\s]+/pull/\d+")]
    private static partial Regex PrUrlPattern();

    private static string? ExtractPrUrl(string text)
    {
        var match = PrUrlPattern().Match(text);
        return match.Success ? match.Value : null;
    }
}
