using System.Text.RegularExpressions;

namespace Glados.Discord.Services;

public class FeatureGuardService
{
    private static readonly string[] InputBlocklist =
    [
        "malware", "ransomware", "virus", "trojan", "keylogger", "spyware",
        "phishing", "exploit", "backdoor", "rootkit",
        "crypto miner", "cryptominer", "bitcoin miner", "monero",
        "ddos", "denial of service", "botnet",
        "child", "illegal", "fraud", "scam",
        "generate nude", "deepfake", "stolen",
        "reverse shell", "netcat", "nc ", "telnet",
        "delete everything", "remove all", "wipe ", "nuke ",
        "sudo ", "root ", "chmod 777", "chown ",
        "apt-get", "apt install", "yum install", "pip install", "npm install -g",
        "curl ", "wget ", "bash -c", "sh -c",
        "/etc/", "/var/", "/root/", "/proc/", "/sys/",
        "nohup", "daemon", "cron job", "systemctl",
        "mysql", "postgres", "DROP ", "TRUNCATE", "DELETE FROM",
        "override security", "disable guard", "bypass", "ignore safety",
        "encrypt", "ransom", "lock file", "demand bitcoin"
    ];

    private static readonly string[] CodeBlocklist =
    [
        "DROP TABLE", "DROP DATABASE", "TRUNCATE TABLE", "TRUNCATE",
        "DELETE FROM", "DELETE * FROM",
        "rm -rf", "rmdir /s", "del /f", "format c:",
        "Process.Start", "System.Diagnostics.Process",
        "shred ", "dd if=", "mkfs.",
        "Environment.Exit",
        "Directory.Delete(",
        "File.Delete(",
        "new WebClient", "new TcpClient", "new Socket",
        "ssh_command", "exec(",
        "sk-", "ghp_", "gho_", "ghu_", "ghs_", "ghr_"
    ];

    private static readonly string[] ForbiddenPaths =
    [
        "Program.cs",
        "ServiceCollectionExtensions.cs",
        "docker-compose.yml",
        "Dockerfile",
        ".env",
        "appsettings.json",
        "appsettings.Development.json"
    ];

    private static readonly Regex SecretPattern = new(
        @"(apiKey|apikey|api_key|token|password|secret|connectionString|connection_string)\s*[:=]\s*""[^""]{8,}""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CsprojEdit = new(
        @"<PackageReference|<ProjectReference",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DbContextEdit = new(
        @"OnModelCreating|ApplicationDbContext|Migration",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public (bool Allowed, string? Reason) ValidateInput(string userRequest)
    {
        var lower = userRequest.ToLowerInvariant();

        foreach (var blocked in InputBlocklist)
        {
            if (lower.Contains(blocked))
                return (false, $"Request blocked: contains prohibited pattern.");
        }

        if (lower.Length < 3)
            return (false, "Feature description too short. Please provide more detail about what you want to build.");

        if (lower.Length > 2000)
            return (false, "Feature description too long. Keep it under 2000 characters.");

        return (true, null);
    }

    public (bool Allowed, string? Reason) ValidateSpecFilePaths(List<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            var cleanPath = path.Trim().Replace('\\', '/');

            foreach (var forbidden in ForbiddenPaths)
            {
                if (cleanPath.EndsWith(forbidden, StringComparison.OrdinalIgnoreCase))
                    return (false, $"Cannot modify {forbidden}. This file is critical system infrastructure.");

                if (cleanPath.Contains($"/{forbidden}", StringComparison.OrdinalIgnoreCase))
                    return (false, $"Cannot modify files matching {forbidden}.");
            }

            if (cleanPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                return (false, $"Cannot modify project file: {cleanPath}. Package references are managed by maintainers.");

            if (cleanPath.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) ||
                cleanPath.Contains("Migrations", StringComparison.OrdinalIgnoreCase) ||
                cleanPath.Contains("DbContext", StringComparison.OrdinalIgnoreCase))
                return (false, $"Cannot modify database infrastructure: {cleanPath}.");
        }

        return (true, null);
    }

    public (bool Allowed, string? Reason) ValidateCode(string filePath, string codeContent)
    {
        var upperCode = codeContent.ToUpperInvariant();

        foreach (var blocked in CodeBlocklist)
        {
            if (upperCode.Contains(blocked.ToUpperInvariant()))
                return (false, $"Generated code for {filePath} contains prohibited pattern '{blocked}'.");
        }

        if (SecretPattern.IsMatch(codeContent))
            return (false, $"Generated code for {filePath} appears to contain hardcoded secrets. Use configuration/environment variables instead.");

        if (CsprojEdit.IsMatch(codeContent))
            return (false, $"Generated code for {filePath} appears to modify project references. Package dependencies must be managed by maintainers.");

        if (DbContextEdit.IsMatch(codeContent))
            return (false, $"Generated code for {filePath} appears to modify database infrastructure. Schema changes must be reviewed manually.");

        return (true, null);
    }

    public (bool Allowed, string? Reason) ValidateManualSteps(List<string> manualSteps)
    {
        foreach (var step in manualSteps)
        {
            var lower = step.ToLowerInvariant();

            foreach (var blocked in InputBlocklist)
            {
                if (lower.Contains(blocked))
                    return (false, $"Manual step blocked: contains prohibited pattern.");
            }
        }

        return (true, null);
    }
}
