using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Octokit;

namespace Glados.Discord.Services;

public partial class GitHubService
{
    private readonly IConfiguration _configuration;

    public GitHubService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (bool Configured, string Owner, string Repo, string Token) GetCredentials()
    {
        var token = _configuration["GitHub:Token"];
        var owner = _configuration["GitHub:Owner"] ?? "ramon96";
        var repo = _configuration["GitHub:Repo"] ?? "DiscordbotV2";

        return (
            Configured: !string.IsNullOrWhiteSpace(token),
            Owner: owner,
            Repo: repo,
            Token: token ?? ""
        );
    }

    public async Task<string> CreateFeaturePullRequestAsync(
        string branchName,
        string prTitle,
        string prBody,
        List<(string Path, string Content)> files,
        CancellationToken ct = default)
    {
        var creds = GetCredentials();
        if (!creds.Configured)
            throw new InvalidOperationException("GitHub token not configured. Set GitHub:Token in configuration.");

        var client = new GitHubClient(new ProductHeaderValue("GLADOS-FeatureBot"))
        {
            Credentials = new Credentials(creds.Token)
        };

        var mainRef = await client.Git.Reference.Get(creds.Owner, creds.Repo, "heads/main");
        var mainSha = mainRef.Object.Sha;

        var mainCommit = await client.Git.Commit.Get(creds.Owner, creds.Repo, mainSha);

        var newRef = new NewReference($"refs/heads/{branchName}", mainSha);
        await client.Git.Reference.Create(creds.Owner, creds.Repo, newRef);

        var treeItems = new List<NewTreeItem>();
        foreach (var file in files)
        {
            var blob = new NewBlob
            {
                Content = file.Content,
                Encoding = EncodingType.Utf8
            };
            var blobRef = await client.Git.Blob.Create(creds.Owner, creds.Repo, blob);

            treeItems.Add(new NewTreeItem
            {
                Path = file.Path,
                Mode = "100644",
                Type = TreeType.Blob,
                Sha = blobRef.Sha
            });
        }

        var newTree = new NewTree
        {
            BaseTree = mainCommit.Tree.Sha
        };
        foreach (var item in treeItems)
        {
            newTree.Tree.Add(item);
        }
        var tree = await client.Git.Tree.Create(creds.Owner, creds.Repo, newTree);

        var newCommit = new NewCommit(prTitle, tree.Sha, mainSha);
        var commit = await client.Git.Commit.Create(creds.Owner, creds.Repo, newCommit);

        await client.Git.Reference.Update(creds.Owner, creds.Repo, $"heads/{branchName}", new ReferenceUpdate(commit.Sha));

        var pullRequest = new NewPullRequest(prTitle, branchName, "main")
        {
            Body = prBody
        };
        var pr = await client.PullRequest.Create(creds.Owner, creds.Repo, pullRequest);

        return pr.HtmlUrl;
    }

    public async Task AddCommentAsync(string prUrl, string comment, CancellationToken ct = default)
    {
        var creds = GetCredentials();
        if (!creds.Configured)
            return;

        var client = new GitHubClient(new ProductHeaderValue("GLADOS-FeatureBot"))
        {
            Credentials = new Credentials(creds.Token)
        };

        var match = PrUrlRegex().Match(prUrl);
        if (!match.Success)
            return;

        var owner = match.Groups[1].Value;
        var repo = match.Groups[2].Value;
        var prNumber = int.Parse(match.Groups[3].Value);

        await client.Issue.Comment.Create(owner, repo, prNumber, comment);
    }

    public static string SanitizeBranchName(string description)
    {
        var slug = BranchSanitizeRegex().Replace(description.ToLowerInvariant(), "");
        slug = WhitespaceRegex().Replace(slug, "-");
        slug = slug.Trim('-');
        if (slug.Length > 50) slug = slug[..50];
        return $"ai-{slug}";
    }

    [GeneratedRegex(@"github\.com/([^/]+)/([^/]+)/pull/(\d+)")]
    private static partial Regex PrUrlRegex();

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex BranchSanitizeRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
