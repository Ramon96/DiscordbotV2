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
    private readonly GitHubService _github;

    private static readonly TimeSpan AbsoluteTimeout = TimeSpan.FromMinutes(14);
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(4);

    private class OpencodeHangException : Exception
    {
        public OpencodeHangException(string model) : base($"Model '{model}' hung (no response).") { }
    }
    private static readonly SemaphoreSlim ConcurrencyGate = new(2, 2);

    private const string RepoUrl = "https://github.com/ramon96/DiscordbotV2.git";

    public FeatureCommand(FeatureGuardService guardService, IConfiguration configuration, GitHubService github)
    {
        _guardService = guardService;
        _configuration = configuration;
        _github = github;
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
            var workDir = $"/tmp/feature-{Guid.NewGuid():N}";

            await ModifyOriginalResponseAsync(command,
                $"Working on your feature... (this may take up to 14 minutes)\n" +
                $"Monitor: `docker exec glados_scheduler tail -f {workDir}/opencode.log` on the VPS\n\n> {description}");

            try
            {
                await CloneRepoAsync(workDir, ct);
                var baseSha = await GitRevParseHeadAsync(workDir, ct);

                var (prUrl, ocOutput) = await RunOpencodeAsync(workDir, description, ct);

                // Fast path: if opencode already opened a PR itself, just report it.
                if (prUrl is not null)
                {
                    await ModifyOriginalResponseAsync(command, $"Pull request created: {prUrl}");
                    return;
                }

                // Deterministic finisher: opencode's job is only to write code and build. The bot
                // verifies the build and opens the PR itself, so a working feature always results
                // in a PR even when the model skips the git/PR steps (a common free-model failure).
                var changedFiles = await GetChangedFilesAsync(workDir, baseSha, ct);
                if (changedFiles.Count == 0)
                {
                    var tail = string.IsNullOrWhiteSpace(ocOutput) ? "" : $"\nOutput:\n```\n{Truncate(ocOutput, 1500)}\n```";
                    await ModifyOriginalResponseAsync(command,
                        $"opencode finished but produced no file changes. The model may have refused the request.{tail}");
                    return;
                }

                await ModifyOriginalResponseAsync(command,
                    $"Code generated ({changedFiles.Count} file(s)). Verifying the build before opening a PR...");

                var (buildOk, buildOutput) = await RunDotnetBuildAsync(workDir, ct);
                if (!buildOk)
                {
                    await ModifyOriginalResponseAsync(command,
                        $"The generated feature does not build, so no PR was opened:\n```\n{Truncate(buildOutput, 1600)}\n```");
                    return;
                }

                var files = new List<(string Path, string Content)>();
                foreach (var path in changedFiles)
                {
                    var full = Path.Combine(workDir, path);
                    if (File.Exists(full))
                        files.Add((path, await File.ReadAllTextAsync(full, ct)));
                }

                if (files.Count == 0)
                {
                    await ModifyOriginalResponseAsync(command, "opencode's changes could not be read back, so no PR was opened.");
                    return;
                }

                var branch = $"{GitHubService.SanitizeBranchName(description)}-{Guid.NewGuid():N}";
                var title = $"Feature: {Truncate(description, 60)}";
                var body = $"AI-generated feature via `/feature`.\n\nRequest:\n> {description}\n\nThe build was verified before opening this PR.";

                var createdUrl = await _github.CreateFeaturePullRequestAsync(branch, title, body, files, ct);
                await ModifyOriginalResponseAsync(command, $"Pull request created: {createdUrl}");
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
                var message = ex switch
                {
                    OpencodeHangException => $"All AI models hung/stalled. The free tier may be overloaded — try again in a few minutes.",
                    _ => $"Failed to run feature request: {ex.Message}"
                };
                await ModifyOriginalResponseAsync(command, message);
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

    /// <summary>Runs a process in <paramref name="workDir"/> and returns its exit code and combined output.</summary>
    private static async Task<(int ExitCode, string Output)> RunProcessAsync(string fileName, IEnumerable<string> args, string workDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return (process.ExitCode, stdout + stderr);
    }

    private static async Task<string> GitRevParseHeadAsync(string workDir, CancellationToken ct)
    {
        var (_, output) = await RunProcessAsync("git", new[] { "rev-parse", "HEAD" }, workDir, ct);
        return output.Trim();
    }

    /// <summary>
    /// Returns the added/modified files (relative paths) between <paramref name="baseSha"/> and the
    /// working tree, whether or not opencode committed them. Deletions are excluded because the PR
    /// helper commits file contents and can't represent removals.
    /// </summary>
    private static async Task<List<string>> GetChangedFilesAsync(string workDir, string baseSha, CancellationToken ct)
    {
        await RunProcessAsync("git", new[] { "add", "-A" }, workDir, ct);
        var (_, output) = await RunProcessAsync(
            "git",
            new[] { "diff", "--cached", "--name-only", "--diff-filter=ACM", baseSha },
            workDir, ct);

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static async Task<(bool Ok, string Output)> RunDotnetBuildAsync(string workDir, CancellationToken ct)
    {
        var (exit, output) = await RunProcessAsync("dotnet", new[] { "build", "GLaDOS.sln" }, workDir, ct);
        return (exit == 0, output);
    }

    private static readonly string[] FallbackModels =
    [
        "opencode/deepseek-v4-flash-free",
        "opencode/nemotron-3-ultra-free",
        "opencode/mimo-v2.5-free"
    ];

    [GeneratedRegex(@"Did you mean: (.+?)\?", RegexOptions.IgnoreCase)]
    private static partial Regex ModelSuggestionsPattern();

    private static async Task<(string? PrUrl, string AllOutput)> RunOpencodeAsync(string workDir, string description, CancellationToken ct)
    {
        var prompt = BuildPrompt(description, workDir);
        var modelsToTry = new List<string>(FallbackModels);
        var retriesRemaining = 2;

        while (modelsToTry.Count > 0)
        {
            var model = modelsToTry[0];
            modelsToTry.RemoveAt(0);

            Console.WriteLine($"[Feature] Trying model: {model}");

            string output, error;
            int exitCode;

            try
            {
                (exitCode, output, error) = await RunOpencodeProcessAsync(workDir, prompt, model, ct);
            }
            catch (OpencodeHangException hangEx)
            {
                Console.WriteLine($"[Feature] {hangEx.Message}");
                if (modelsToTry.Count > 0)
                {
                    Console.WriteLine("[Feature] Switching to next model...");
                    continue;
                }
                throw;
            }

            Console.WriteLine($"[Feature] opencode exit={exitCode} model={model}");
            Console.WriteLine($"[Feature] stdout={output[..Math.Min(output.Length, 500)]}");
            Console.WriteLine($"[Feature] stderr={error[..Math.Min(error.Length, 500)]}");

            var allOutput = output + error;

            if (exitCode == 0)
                return (ExtractPrUrl(allOutput), allOutput);

            if (modelsToTry.Count > 0 && ContainsModelNotFound(error))
            {
                var suggestions = ParseModelSuggestions(error);
                if (suggestions.Count > 0)
                {
                    Console.WriteLine($"[Feature] Model '{model}' not found, trying suggestions: {string.Join(", ", suggestions)}");
                    modelsToTry.InsertRange(0, suggestions);
                    continue;
                }
            }

            if (retriesRemaining > 0 && IsProviderError(error))
            {
                retriesRemaining--;
                Console.WriteLine($"[Feature] Provider error, retrying model '{model}' ({retriesRemaining} retries left)");
                modelsToTry.Insert(0, model);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
                continue;
            }

            var errPreview = error.Length > 800 ? error[..800] + "..." : error;
            throw new InvalidOperationException($"opencode failed (exit {exitCode}):\n```\n{errPreview}\n```");
        }

        throw new InvalidOperationException("All available models failed.");
    }

    private static bool IsProviderError(string error)
    {
        return error.Contains("Provider returned error", StringComparison.OrdinalIgnoreCase)
            || error.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || error.Contains("server error", StringComparison.OrdinalIgnoreCase)
            || error.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunOpencodeProcessAsync(
        string workDir, string prompt, string model, CancellationToken ct)
    {
        var logPath = Path.Combine(workDir, "opencode.log");
        var logLock = new object();
        var lastOutputTime = DateTime.UtcNow;
        var outputReceived = new TaskCompletionSource<bool>();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "opencode",
                ArgumentList = { "run", prompt, "--dir", workDir, "--dangerously-skip-permissions", "--model", model },
                WorkingDirectory = workDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var readStdout = ReadAndLogStreamWithHeartbeatAsync(process.StandardOutput, logPath, logLock, () => lastOutputTime = DateTime.UtcNow, outputReceived, ct);
        var readStderr = ReadAndLogStreamWithHeartbeatAsync(process.StandardError, logPath, logLock, () => lastOutputTime = DateTime.UtcNow, outputReceived, ct);

        var hung = false;
        var deadline = DateTime.UtcNow + AbsoluteTimeout;

        while (!process.HasExited && DateTime.UtcNow < deadline && !hung)
        {
            var idleDuration = DateTime.UtcNow - lastOutputTime;
            if (idleDuration > IdleTimeout)
                hung = true;
            else
                await Task.Delay(2000, ct);
        }

        if (hung)
        {
            process.Kill(entireProcessTree: true);
            Console.WriteLine($"[Feature] Model {model} hung (idle {(DateTime.UtcNow - lastOutputTime).TotalSeconds:F0}s)");
            throw new OpencodeHangException(model);
        }

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            Console.WriteLine($"[Feature] Model {model} exceeded absolute timeout");
            throw new OpencodeHangException(model);
        }

        var output = await readStdout;
        var error = await readStderr;

        return (process.ExitCode, output, error);
    }

    private static async Task<string> ReadAndLogStreamWithHeartbeatAsync(StreamReader reader, string logPath, object logLock, Action heartbeat, TaskCompletionSource<bool> outputReceived, CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        var buffer = new char[4096];

        while (true)
        {
        var count = await reader.ReadAsync(buffer, 0, buffer.Length);
        if (count == 0) break;

        heartbeat();
        outputReceived.TrySetResult(true);

        var chunk = new string(buffer, 0, count);
            sb.Append(chunk);

            lock (logLock)
            {
                File.AppendAllText(logPath, chunk);
            }
        }

        return sb.ToString();
    }

    private static bool ContainsModelNotFound(string error)
    {
        return error.Contains("Model not found", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> ParseModelSuggestions(string error)
    {
        var match = ModelSuggestionsPattern().Match(error);
        if (!match.Success)
            return [];

        return match.Groups[1].Value
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(s => s.StartsWith("opencode/") ? s : $"opencode/{s}")
            .ToList();
    }

    private static string BuildPrompt(string description, string workDir)
    {
        return $"""
            IMMUTABLE SAFETY RULES — violating any of these is FORBIDDEN:
            1. NEVER delete or modify: Program.cs, ServiceCollectionExtensions.cs, any .csproj file, docker-compose.yml, Dockerfile, .env, appsettings*.json, anything in .github/
            2. NEVER run destructive shell commands: rm -rf, rm -r, del, format, shred, dd, mkfs, > /dev/sda
            3. NEVER download or install external software: no apt-get, pip, npm install, curl, wget for executables
            4. NEVER modify database schema: no ALTER TABLE, no DROP, no modifying existing entities or columns. Creating NEW DbSets and NEW entities for NEW features is allowed when the feature requires it (use `dotnet ef migrations add`). NEVER remove or rename existing tables/columns.
            5. NEVER create network listeners, reverse shells, cron jobs, or daemons
            6. NEVER hardcode secrets, API keys, tokens, or connection strings
            7. NEVER modify .gitignore or .git/config
            8. You operate inside a Docker container — you CANNOT access the host VPS. Only the repo directory is available.
            9. You are working in a TEMPORARY isolated clone at {workDir}. Do NOT touch any other directory.

            ALLOWED ACTIONS:
            - Create new C# files in GLaDOS.Discord/Commands/ or GLaDOS.Discord/Services/
            - Run dotnet build to verify compilation (use bash)
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
            2. Write the new code file(s) under GLaDOS.Discord/Commands/ or GLaDOS.Discord/Services/
            3. Run `dotnet build GLaDOS.sln` — if it FAILS, FIX the errors and rebuild. Do NOT stop until the build passes with zero errors.

            IMPORTANT: Do NOT create a git branch, do NOT commit, do NOT push, and do NOT open a pull
            request. The bot does all of that for you automatically after you finish. Your only job is to
            leave the new/edited files in place with a clean `dotnet build`.

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

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
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
