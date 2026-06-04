using System.Text.Json;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Glados.Discord.AI;
using Glados.Discord.Services;
using Microsoft.Extensions.Configuration;

namespace Glados.Discord.Commands;

public partial class FeatureCommand : IDiscordCommand
{
    private readonly AIService _aiService;
    private readonly GitHubService _gitHubService;
    private readonly FeatureGuardService _guardService;
    private readonly IConfiguration _configuration;

    private const string SpecModel = "nemotron-3-super-free";
    private const string CodeModel = "nemotron-3-super-free";

    private const string SpecSystemPrompt =
        """
        You are an expert software architect. Transform user feature requests into precise technical specifications for a C# .NET 10.0 Discord bot codebase.

        Codebase architecture:
        - Solution GLaDOS.sln: GLaDOS.Core (domain models/DTOs), GLaDOS.Infra (EF Core/PostgreSQL), GLaDOS.Discord (commands/services), GLaDOS.Scheduler (ASP.NET host/Hangfire)
        - Commands implement IDiscordCommand: string Name, SlashCommandProperties GetCommandDefinition(), Task ExecuteAsync(SocketSlashCommand command, CancellationToken ct)
        - Commands register in ServiceCollectionExtensions.cs via DI (AddSingleton<IDiscordCommand, T>)
        - Services in GLaDOS.Discord/Services/, registered via DI
        - AIService: Glados.Discord.AI.AIService, method SendAsync(systemPrompt, userPrompt, model, maxTokens, temperature, ct) returns string?
        - GitHubService: Glados.Discord.Services.GitHubService
        - Commands inject IServiceProvider + IConfiguration via constructor, create scope for DbContext (ApplicationDbContext)
        - Discord.Net 3.18.0, Entity Framework Core 10 with PostgreSQL

        HARD CONSTRAINTS — NEVER violate these:
        - NEVER add NuGet packages — note them in manualSteps instead
        - NEVER modify Program.cs, ServiceCollectionExtensions.cs, .csproj, docker-compose.yml, Dockerfile, .env, appsettings
        - NEVER create EF migrations or modify ApplicationDbContext/DbContext
        - NEVER modify existing files — create NEW files only
        - File paths relative to repo root (e.g., GLaDOS.Discord/Commands/MyCommand.cs)

        Output ONLY valid JSON (no markdown, no commentary):
        {"title":"string","summary":"string","files":[{"path":"string","description":"string"}],"manualSteps":["string"]}
        """;

    private const string CodeSystemPrompt =
        """
        You are an expert C# .NET 10.0 developer. Write complete, compilable code for Discord bot features using Discord.Net 3.18.0.

        Patterns to follow:
        - IDiscordCommand: string Name, SlashCommandProperties GetCommandDefinition(), Task ExecuteAsync(SocketSlashCommand command, CancellationToken ct=default)
        - Constructor: (IServiceProvider services, IConfiguration configuration) for commands
        - DB: using var scope = _services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        - AI: inject Glados.Discord.AI.AIService, call _aiService.SendAsync(systemPrompt, userPrompt, model, maxTokens, temperature, ct)
        - Interaction: await command.DeferAsync() first, then ModifyOriginalResponseAsync for result
        - EmbedBuilder for rich embeds
        - Minimal comments, early returns for errors
        - All using statements included, namespace Glados.Discord.Commands or Glados.Discord.Services
        - Use GLaDOS.Infra.EntityFramework.ApplicationDbContext for database
        - Use GLaDOS.Core.Domain for domain models

        Output EXACT format for each file:
        ### FILE: path/FileName.cs
        ```csharp
        // complete compilable code
        ```

        CRITICAL:
        - COMPLETE files: namespace, using statements, full class
        - NEVER add PackageReference or modify .csproj
        - NEVER create HttpClient instances in new code — use AIService
        - NEVER new up services — use constructor injection
        - New command classes get registered in DI by the maintainer (note in manualSteps)
        """;

    public FeatureCommand(AIService aiService, GitHubService gitHubService, FeatureGuardService guardService, IConfiguration configuration)
    {
        _aiService = aiService;
        _gitHubService = gitHubService;
        _guardService = guardService;
        _configuration = configuration;
    }

    public string Name => "feature";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Request a feature. The bot analyzes, builds, and submits a pull request automatically.")
            .AddOption("description", ApplicationCommandOptionType.String, "Describe the feature you want to add to the bot", isRequired: true)
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
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

        var (githubConfigured, _, _, _) = _gitHubService.GetCredentials();
        if (!githubConfigured)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = "GitHub integration not configured. Set `GitHub:Token`, `GitHub:Owner`, and `GitHub:Repo` in the bot configuration.");
            return;
        }

        await command.ModifyOriginalResponseAsync(props =>
            props.Content = $"Analyzing your feature request and getting to work...\n\n> {description}");

        var spec = await GenerateSpecAsync(description, cancellationToken);
        if (spec is null)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = "Failed to analyze the feature request. The AI service may be unavailable. Please try again later.");
            return;
        }

        var specFiles = spec.Files.Select(f => f.Path).ToList();
        var (specOk, specReason) = _guardService.ValidateSpecFilePaths(specFiles);
        if (!specOk)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"Spec blocked: {specReason}");
            return;
        }

        var (stepsOk, stepsReason) = _guardService.ValidateManualSteps(spec.ManualSteps);
        if (!stepsOk)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"Manual steps blocked: {stepsReason}");
            return;
        }

        await command.ModifyOriginalResponseAsync(props =>
            props.Content = $"Spec designed. **{spec.Title}**\n\nGenerating code for {spec.Files.Count} file(s)...");

        var specJson = JsonSerializer.Serialize(spec);
        var codeFiles = await GenerateCodeAsync(specJson, spec.Files, cancellationToken);
        if (codeFiles is null || codeFiles.Count == 0)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = "Failed to generate code. The AI service may be unavailable. Please try again later.");
            return;
        }

        foreach (var file in codeFiles)
        {
            var (codeOk, codeReason) = _guardService.ValidateCode(file.Path, file.Content);
            if (!codeOk)
            {
                await command.ModifyOriginalResponseAsync(props =>
                    props.Content = $"Code validation blocked in `{file.Path}`: {codeReason}");
                return;
            }
        }

        try
        {
            var branchName = GitHubService.SanitizeBranchName(spec.Title);
            var prBody = BuildPrBody(spec, codeFiles);

            var prUrl = await _gitHubService.CreateFeaturePullRequestAsync(
                branchName, spec.Title, prBody, codeFiles, cancellationToken);

            if (spec.ManualSteps.Count > 0)
            {
                var manualComment = "## Manual Steps Required\n\n" +
                    string.Join("\n", spec.ManualSteps.Select(s => $"- {s}")) +
                    "\n\n*These steps must be completed by a human maintainer before this PR can be merged.*";
                await _gitHubService.AddCommentAsync(prUrl, manualComment, cancellationToken);
            }

            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"Pull request created: {prUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PR creation failed: {ex.Message}");
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"Failed to create the pull request: {ex.Message}");
        }
    }

    private async Task<FeatureSpec?> GenerateSpecAsync(string userRequest, CancellationToken ct)
    {
        var prompt = $"""
            Feature request: "{userRequest}"

            Output ONLY the JSON specification:
            """;

        var response = await _aiService.SendAsync(SpecSystemPrompt, prompt, SpecModel, maxTokens: 2000, temperature: 0.7, ct: ct);

        if (response is null) return null;

        try
        {
            var json = ExtractJson(response);
            var spec = JsonSerializer.Deserialize<FeatureSpec>(json);
            if (spec is null || string.IsNullOrWhiteSpace(spec.Title) || spec.Files is null || spec.Files.Count == 0)
                return null;
            return spec;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse spec JSON: {ex.Message}\nResponse: {response}");
            return null;
        }
    }

    private async Task<List<(string Path, string Content)>?> GenerateCodeAsync(string specJson, List<SpecFile> files, CancellationToken ct)
    {
        var fileList = string.Join("\n", files.Select(f => $"- {f.Path}: {f.Description}"));

        var prompt = $"""
            Technical Specification:
            {specJson}

            Generate code for each of these files:
            {fileList}

            Output each file in the EXACT format specified.
            """;

        var response = await _aiService.SendAsync(CodeSystemPrompt, prompt, CodeModel, maxTokens: 8000, temperature: 0.3, ct: ct);

        if (response is null) return null;

        return ParseCodeFiles(response);
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];
        return text;
    }

    [GeneratedRegex(@"###\s*FILE:\s*(.+?)\n```(?:csharp|cs)\n(.+?)```", RegexOptions.Singleline)]
    private static partial Regex CodeFilePattern();

    private static List<(string Path, string Content)> ParseCodeFiles(string aiResponse)
    {
        var result = new List<(string Path, string Content)>();
        var matches = CodeFilePattern().Matches(aiResponse);
        foreach (Match match in matches)
        {
            var path = match.Groups[1].Value.Trim();
            var content = match.Groups[2].Value.Trim();
            if (!string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(content))
                result.Add((path, content));
        }
        return result;
    }

    private static string BuildPrBody(FeatureSpec spec, List<(string Path, string Content)> files)
    {
        var lines = new List<string>
        {
            spec.Summary,
            "",
            "## Changes",
            ""
        };

        foreach (var file in spec.Files)
        {
            lines.Add($"- **{file.Path}**: {file.Description}");
        }

        if (spec.ManualSteps.Count > 0)
        {
            lines.Add("");
            lines.Add("## Manual Steps Required");
            lines.Add("");
            foreach (var step in spec.ManualSteps)
            {
                lines.Add($"- {step}");
            }
            lines.Add("");
            lines.Add("*These steps must be completed by a human maintainer before this PR can be merged.*");
        }

        lines.Add("");
        lines.Add("*This PR was auto-generated by the GLaDOS feature bot based on a user request.*");

        return string.Join("\n", lines);
    }
}

public record FeatureSpec(
    string Title,
    string Summary,
    List<SpecFile> Files,
    List<string> ManualSteps
);

public record SpecFile(string Path, string Description);
