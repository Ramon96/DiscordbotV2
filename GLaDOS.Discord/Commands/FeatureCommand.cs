using System.Collections.Concurrent;
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

    private static readonly ConcurrentDictionary<ulong, PendingFeatureRequest> PendingRequests = new();
    private static readonly TimeSpan PendingTimeout = TimeSpan.FromMinutes(30);

    private const string SpecModel = "nemotron-3-super-free";
    private const string CodeModel = "nemotron-3-super-free";

    private const string SpecSystemPrompt =
        """
        Design a spec for a C# .NET 10 Discord.Net bot feature. Output ONLY valid JSON (no reasoning):

        Bot: commands in GLaDOS.Discord/Commands/ implement IDiscordCommand. Services in GLaDOS.Discord/Services/. EF Core PostgreSQL via ApplicationDbContext (scoped). DI in ServiceCollectionExtensions.cs. Discord.Net 3.18.

        Rules: new files only. Never touch .csproj, Program.cs, ServiceCollectionExtensions.cs, docker-compose.yml, DbContext. NuGet pkgs→manualSteps. Paths relative to repo root.

        If vague, ask ≤4 clarifications with options. If clear, clarifications:[].

        Schema: {"title":"...","summary":"...","clarifications":[{"question":"...","options":["..."]}],"files":[{"path":"...","description":"..."}],"manualSteps":["..."]}
        """;

    private const string CodeSystemPrompt =
        """
        Write compilable C# .NET 10 Discord.Net 3.18 code. Output ONLY files (no reasoning).

        IDiscordCommand: Name, GetCommandDefinition(), ExecuteAsync(SocketSlashCommand, ct).
        Inject IServiceProvider+IConfiguration. DB: new scope→ApplicationDbContext. AI: AIService.SendAsync().
        DeferAsync first, ModifyOriginalResponseAsync for result. EmbedBuilder for rich content.
        Namespace Glados.Discord.Commands or Services. All usings included.

        Format per file:
        ### FILE: path/File.cs
        ```csharp
        code
        ```
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

        var (spec, specError) = await GenerateSpecAsync(description, cancellationToken);
        if (spec is null)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = specError ?? "Failed to analyze the feature request. The AI service may be unavailable. Please try again later.");
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

        if (spec.Clarifications is { Count: > 0 })
        {
            await ShowClarificationsAsync(command, spec, description, cancellationToken);
            return;
        }

        await BuildAndCreatePrAsync(command, spec, description, cancellationToken);
    }

    private async Task ShowClarificationsAsync(SocketSlashCommand command, FeatureSpec spec, string description, CancellationToken ct)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];

        PendingRequests[command.User.Id] = new PendingFeatureRequest(
            requestId, spec, description, command.Channel.Id, DateTime.UtcNow);

        var questionsText = new System.Text.StringBuilder();
        questionsText.AppendLine($"**{spec.Title}**\n");
        questionsText.AppendLine(spec.Summary);
        questionsText.AppendLine("\nI need clarification on a few things:\n");

        for (var i = 0; i < spec.Clarifications.Count; i++)
        {
            var q = spec.Clarifications[i];
            questionsText.AppendLine($"{i + 1}. **{q.Question}**");
            if (q.Options is { Count: > 0 })
            {
                questionsText.Append("   Options: ");
                questionsText.AppendLine(string.Join(" / ", q.Options));
            }
            questionsText.AppendLine();
        }

        var button = new ButtonBuilder()
            .WithLabel("Refine Feature")
            .WithCustomId($"feature-refine-{requestId}")
            .WithStyle(ButtonStyle.Primary);

        var component = new ComponentBuilder()
            .WithButton(button)
            .Build();

        await command.ModifyOriginalResponseAsync(props =>
        {
            props.Content = questionsText.ToString();
            props.Components = component;
        });
    }

    public async Task HandleClarifyButtonAsync(SocketMessageComponent component)
    {
        var customId = component.Data.CustomId;
        var requestId = customId["feature-refine-".Length..];

        if (!PendingRequests.TryGetValue(component.User.Id, out var pending) || pending.RequestId != requestId)
        {
            await component.RespondAsync("This clarification request has expired or is not yours. Run `/feature` again.", ephemeral: true);
            return;
        }

        if (DateTime.UtcNow - pending.CreatedAt > PendingTimeout)
        {
            PendingRequests.TryRemove(component.User.Id, out _);
            await component.RespondAsync("This clarification request has timed out. Run `/feature` again.", ephemeral: true);
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle("Feature Clarification")
            .WithCustomId($"feature-clarify-{requestId}");

        var questionCount = Math.Min(pending.Spec.Clarifications.Count, 5);
        for (var i = 0; i < questionCount; i++)
        {
            var q = pending.Spec.Clarifications[i];
            var placeholder = q.Options is { Count: > 0 }
                ? string.Join(" / ", q.Options)
                : "Type your answer...";

            modal.AddTextInput(
                label: q.Question.Length > 45 ? q.Question[..42] + "..." : q.Question,
                customId: $"clarify-{i}",
                placeholder: placeholder.Length > 100 ? placeholder[..97] + "..." : placeholder,
                required: false,
                style: TextInputStyle.Paragraph);
        }

        await component.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleClarifyModalAsync(SocketModal modal)
    {
        var customId = modal.Data.CustomId;
        var requestId = customId["feature-clarify-".Length..];

        if (!PendingRequests.TryGetValue(modal.User.Id, out var pending) || pending.RequestId != requestId)
        {
            await modal.RespondAsync("This clarification request has expired. Run `/feature` again.", ephemeral: true);
            return;
        }

        PendingRequests.TryRemove(modal.User.Id, out _);

        var answers = new Dictionary<string, string>();
        foreach (var input in modal.Data.Components)
        {
            if (!string.IsNullOrWhiteSpace(input.Value))
                answers[input.CustomId] = input.Value;
        }

        await modal.DeferAsync();

        var enrichedDescription = pending.Description;
        if (answers.Count > 0)
        {
            enrichedDescription += "\n\nClarifications from user:\n";
            for (var i = 0; i < pending.Spec.Clarifications.Count; i++)
            {
                var q = pending.Spec.Clarifications[i];
                if (answers.TryGetValue($"clarify-{i}", out var answer))
                    enrichedDescription += $"- {q.Question}: {answer}\n";
            }
        }

        await modal.ModifyOriginalResponseAsync(props =>
            props.Content = $"Refining spec with your clarifications...\n\n> {pending.Spec.Title}");

        var (spec, specError) = await GenerateSpecAsync(enrichedDescription, CancellationToken.None);
        if (spec is null)
        {
            await modal.ModifyOriginalResponseAsync(props =>
                props.Content = specError ?? "Failed to refine the specification. The AI service may be unavailable.");
            return;
        }

        var specFiles = spec.Files.Select(f => f.Path).ToList();
        var (specOk, specReason) = _guardService.ValidateSpecFilePaths(specFiles);
        if (!specOk)
        {
            await modal.ModifyOriginalResponseAsync(props =>
                props.Content = $"Spec blocked: {specReason}");
            return;
        }

        await BuildAndCreatePrAsync(modal, spec, enrichedDescription, CancellationToken.None);
    }

    private async Task BuildAndCreatePrAsync(IDiscordInteraction interaction, FeatureSpec spec, string description, CancellationToken ct)
    {
        await interaction.ModifyOriginalResponseAsync(props =>
            props.Content = $"Spec designed. **{spec.Title}**\n\nGenerating code for {spec.Files.Count} file(s)...");

        var specJson = JsonSerializer.Serialize(spec);
        var codeFiles = await GenerateCodeAsync(specJson, spec.Files, ct);
        if (codeFiles is null || codeFiles.Count == 0)
        {
            await interaction.ModifyOriginalResponseAsync(props =>
                props.Content = "Failed to generate code. The AI service may be unavailable. Please try again later.");
            return;
        }

        foreach (var file in codeFiles)
        {
            var (codeOk, codeReason) = _guardService.ValidateCode(file.Path, file.Content);
            if (!codeOk)
            {
                await interaction.ModifyOriginalResponseAsync(props =>
                    props.Content = $"Code validation blocked in `{file.Path}`: {codeReason}");
                return;
            }
        }

        try
        {
            var branchName = GitHubService.SanitizeBranchName(spec.Title);
            var prBody = BuildPrBody(spec, codeFiles);

            var prUrl = await _gitHubService.CreateFeaturePullRequestAsync(
                branchName, spec.Title, prBody, codeFiles, ct);

            if (spec.ManualSteps.Count > 0)
            {
                var manualComment = "## Manual Steps Required\n\n" +
                    string.Join("\n", spec.ManualSteps.Select(s => $"- {s}")) +
                    "\n\n*These steps must be completed by a human maintainer before this PR can be merged.*";
                await _gitHubService.AddCommentAsync(prUrl, manualComment, ct);
            }

            await interaction.ModifyOriginalResponseAsync(props =>
                props.Content = $"Pull request created: {prUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PR creation failed: {ex.Message}");
            await interaction.ModifyOriginalResponseAsync(props =>
                props.Content = $"Failed to create the pull request: {ex.Message}");
        }
    }

    private async Task<(FeatureSpec? Spec, string? Error)> GenerateSpecAsync(string userRequest, CancellationToken ct)
    {
        var prompt = $"""
            Feature request: "{userRequest}"

            Output ONLY the JSON specification:
            """;

        Console.WriteLine($"[Feature] Sending spec request for: {userRequest[..Math.Min(userRequest.Length, 100)]}");

        var apiKey = _configuration["OpenCode:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return (null, "AI API key not configured. Set `OpenCode:ApiKey` in the bot configuration (`OPENCODE_APIKEY` in `.env`).");

        var response = await _aiService.SendAsync(SpecSystemPrompt, prompt, SpecModel, maxTokens: 1500, temperature: 0.7, ct: ct);

        if (response is null)
        {
            var reason = _aiService.LastError ?? "No response from AI.";
            Console.WriteLine($"[Feature] AIService failed: {reason}");
            return (null, $"AI service failed: {reason}");
        }

        Console.WriteLine($"[Feature] Raw AI response ({response.Length} chars): {response[..Math.Min(response.Length, 300)]}");

        try
        {
            var json = ExtractJson(response);
            if (json.Length == 0)
                return (null, "The AI returned a response but no JSON was found in it. Please try rephrasing your feature request.");

            Console.WriteLine($"[Feature] Extracted JSON ({json.Length} chars)");
            var spec = JsonSerializer.Deserialize<FeatureSpec>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (spec is null)
                return (null, "The AI returned a response but it could not be parsed as a valid specification. Please try rephrasing your feature request.");

            if (string.IsNullOrWhiteSpace(spec.Title))
            {
                Console.WriteLine($"[Feature] Spec has no title. JSON: {json[..Math.Min(json.Length, 200)]}");
                return (null, "The AI returned a specification with no title. Please try rephrasing your feature request.");
            }

            if (spec.Files is null || spec.Files.Count == 0)
            {
                Console.WriteLine($"[Feature] Spec has no files. JSON: {json[..Math.Min(json.Length, 200)]}");
                return (null, "The AI returned a specification with no files to create. Please try a more specific feature request.");
            }

            spec = spec with
            {
                Clarifications = spec.Clarifications ?? new List<Clarification>(),
                ManualSteps = spec.ManualSteps ?? new List<string>()
            };

            Console.WriteLine($"[Feature] Spec OK: title='{spec.Title}', files={spec.Files.Count}, clarifications={spec.Clarifications.Count}");
            return (spec, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Feature] Failed to parse spec JSON: {ex.GetType().Name}: {ex.Message}\nResponse: {response[..Math.Min(response.Length, 500)]}");
            return (null, $"The AI returned a malformed response that could not be parsed.\n\nError: {ex.GetType().Name}: {ex.Message}\n\nPlease try rephrasing your feature request.");
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

internal record PendingFeatureRequest(
    string RequestId,
    FeatureSpec Spec,
    string Description,
    ulong ChannelId,
    DateTime CreatedAt);

public record FeatureSpec(
    string Title,
    string Summary,
    List<Clarification> Clarifications,
    List<SpecFile> Files,
    List<string> ManualSteps
);

public record Clarification(string Question, List<string> Options);

public record SpecFile(string Path, string Description);
