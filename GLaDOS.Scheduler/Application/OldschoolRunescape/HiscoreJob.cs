using GLaDOS.OldschoolRunescape.Clients.Contracts;
using Hangfire;

namespace GLaDOS.Scheduler.Application.OldschoolRunescape;

[DisableConcurrentExecution(60 * 30)] // 30 minutes
[AutomaticRetry(Attempts = 1)]
public class HiscoreJob
{
    private readonly ILogger<HiscoreJob> _logger;
    private readonly IOldschoolRunescapeClient _client;

    public HiscoreJob(ILogger<HiscoreJob> logger, IOldschoolRunescapeClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task ExecuteAsync(string username)
    {
        _logger.LogInformation("Starting hiscore job for user: {Username}", username);

        // get all the players from the database (not implemented here)
        
        // loop through each player and fetch hiscores
        
        // for each player store the hiscores in the database (not implemented here)
        
        var hiscores = await _client.GetHiScoresByUsernameAsync(username);

        // Process hiscores as needed

        _logger.LogInformation("Completed hiscore job for user: {Username}", username);
    }
}