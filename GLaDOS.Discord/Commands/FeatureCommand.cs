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

    private static readonly TimeSpan OpencodeTimeout = TimeSpan.FromMinutes(14);
    private static readonly SemaphoreSlim ConcurrencyGate = new(2, 2);

    private const string RepoUrl = "https://github.com/ramon96/DiscordbotV2.git";

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

        if (!ConcurrencyGate.Wait(0))
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = "The feature workshop is busy (max 2 concurrent requests). Please wait for a running feature to finish and try again.");
            return;
        }

        try
        {
            await ModifyOriginalResponseAsync(command, $"Working on your feature... (this may take up to 14 minutes)\n\n> {description}");

            var workDir = $"/tmp/feature-{Guid.NewGuid():N}";
            try
            {
                await CloneRepoAsync(workDir, ct);

                var prUrl = await RunOpencodeAsync(workDir, description, ct);

                if (prUrl is not null)
                    await ModifyOriginalResponseAsync(command, $"Pull request created: {prUrl}");
                else
                    await ModifyOriginalResponseAsync(command, $"Done! Check the repo for a new PR.");
            }
            finally
            {
                TryDeleteDirectory(workDir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Feature] Failed: {ex}");
            try
            {
                await ModifyOriginalResponseAsync(command, $"Failed to run feature request: {ex.Message}");
            }
            catch { }
        }
        finally
        {
            ConcurrencyGate.Release();
        }
    }

    private static async Task CloneRepoAsync(string workDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            ArgumentList = { "clone", "--depth", "1", RepoUrl, workDir },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var err = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException($"git clone failed (exit {process.ExitCode}): {err}");
        }
    }

    private static async Task<string?> RunOpencodeAsync(string workDir, string description, CancellationToken ct)
    {
        var prompt = BuildPrompt(description, workDir);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "opencode",
                ArgumentList = { "run", prompt, "--dir", workDir, "--dangerously-skip-permissions", "--model", "opencode/nemotron-3-super-free" },
                WorkingDirectory = workDir,
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
            throw new TimeoutException("Timed out after 14 minutes. Try breaking the feature into smaller pieces.");
        }

        var output = await outputTask;
        var error = await errorTask;

        Console.WriteLine($"[Feature] opencode exit={process.ExitCode}");

        if (process.ExitCode != 0)
        {
            var errPreview = error.Length > 800 ? error[..800] + "..." : error;
            throw new InvalidOperationException($"opencode failed (exit {process.ExitCode}):\n```\n{errPreview}\n```");
        }

        return ExtractPrUrl(output + error);
    }

    private static string BuildPrompt(string description, string workDir)
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
            8. You operate inside a Docker container — you CANNOT access the host VPS. Only the repo directory is available.
            9. You are working in a TEMPORARY isolated clone at {workDir}. Do NOT touch any other directory.

            ALLOWED ACTIONS:
            - Create new C# files in GLaDOS.Discord/Commands/ or GLaDOS.Discord/Services/
            - Run dotnet build to verify compilation (use bash)
            - Create git branches, commit, push, and open PRs (use bash)
            - Read any existing files to understand the codebase

            NOTE: New command classes that implement IDiscordCommand are automatically registered in DI.
            No manual registration needed — just create the class and it works on next deploy.

            CODEBASE CONVENTIONS — violating these will introduce bugs:
            1. N+1 QUERIES: NEVER load entities in a loop. Always load ALL records in ONE query using
               .Include() + .AsSplitQuery(). One DbContext, one SaveChanges() at the end. NEVER create
               a new scope/DbContext per record.
            2. DISCORD REGISTRATION: NEVER modify CommandHandlerService.cs. Commands auto-register via
               IDiscordCommand. If you MUST modify registration logic, use BulkOverwriteApplicationCommandAsync
               — NEVER call CreateApplicationCommandAsync in a loop.
            3. SINGLE RESPONSIBILITY: New code goes in GLaDOS.Discord/Commands/ or GLaDOS.Discord/Services/.
               GLaDOS.Scheduler/ is for background jobs only. Keep Discord commands out of the scheduler project.
            4. EXISTING PATTERNS: Before writing code, use Read to study 2-3 existing command files to match
               naming conventions, using statements, constructor patterns, and error handling style.

            WORKFLOW (follow in order — do NOT skip steps):
            1. Read relevant existing code to understand patterns and conventions
            2. Create a new git branch with a descriptive name based on the feature
            3. Write the new code files
            4. Run `dotnet build GLaDOS.sln` in the repo — if it FAILS, FIX the errors and try again. DO NOT proceed to step 5 until the build passes with zero errors.
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

    private static async Task ModifyOriginalResponseAsync(SocketSlashCommand command, string content)
    {
        try
        {
            await command.ModifyOriginalResponseAsync(props => props.Content = content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Feature] Failed to update response: {ex.Message}");
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Feature] Failed to clean up {path}: {ex.Message}");
        }
    }
}
