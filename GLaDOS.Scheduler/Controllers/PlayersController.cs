using Glados.Discord.Commands;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Infra.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("api/players")]
[Authorize]
public class PlayersController : ControllerBase
{
    private const string OverallSkill = "Overall";

    private readonly ApplicationDbContext _dbContext;

    public PlayersController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetLeaderboard(CancellationToken cancellationToken)
    {
        var usernames = await _dbContext.Set<OldschoolRunescapeUser>()
            .Select(user => new { user.Id, user.Username })
            .ToDictionaryAsync(user => user.Id, user => user.Username, cancellationToken);

        var overall = await _dbContext.Set<OldschoolRunescapeStat>()
            .Where(stat => stat.Name == OverallSkill)
            .Select(stat => new
            {
                stat.OldschoolRunescapeUserId,
                stat.Level,
                stat.Experience,
                stat.Rank,
            })
            .ToListAsync(cancellationToken);

        // Earliest "Overall" snapshot within the last week is the baseline for weekly XP gains.
        var weekAgo = DateTime.UtcNow.Date.AddDays(-7);
        var weekBaseline = (await _dbContext.Set<OldschoolRunescapeStatsSnapshot>()
                .Where(snapshot => snapshot.Name == OverallSkill && snapshot.SnapshotDate >= weekAgo)
                .Select(snapshot => new
                {
                    snapshot.OldschoolRunescapeUserId,
                    snapshot.SnapshotDate,
                    snapshot.Experience,
                })
                .ToListAsync(cancellationToken))
            .GroupBy(snapshot => snapshot.OldschoolRunescapeUserId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(snapshot => snapshot.SnapshotDate).First().Experience);

        var leaderboard = overall
            .Where(stat => usernames.ContainsKey(stat.OldschoolRunescapeUserId))
            .Select(stat => new PlayerSummary(
                stat.OldschoolRunescapeUserId,
                usernames[stat.OldschoolRunescapeUserId],
                stat.Level,
                stat.Experience,
                stat.Rank,
                weekBaseline.TryGetValue(stat.OldschoolRunescapeUserId, out var baseline)
                    ? stat.Experience - baseline
                    : null))
            .OrderByDescending(player => player.TotalXp)
            .ToList();

        return Ok(leaderboard);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayer(Guid id, CancellationToken cancellationToken)
    {
        var username = await _dbContext.Set<OldschoolRunescapeUser>()
            .Where(user => user.Id == id)
            .Select(user => user.Username)
            .FirstOrDefaultAsync(cancellationToken);

        if (username is null)
        {
            return NotFound();
        }

        var skills = await _dbContext.Set<OldschoolRunescapeStat>()
            .Where(stat => stat.OldschoolRunescapeUserId == id)
            .OrderBy(stat => stat.SkillId)
            .Select(stat => new PlayerSkill(stat.SkillId, stat.Name, stat.Level, stat.Experience, stat.Rank))
            .ToListAsync(cancellationToken);

        var xpHistory = await _dbContext.Set<OldschoolRunescapeStatsSnapshot>()
            .Where(snapshot => snapshot.OldschoolRunescapeUserId == id && snapshot.Name == OverallSkill)
            .OrderBy(snapshot => snapshot.SnapshotDate)
            .Select(snapshot => new XpPoint(snapshot.SnapshotDate, snapshot.Experience, snapshot.Level))
            .ToListAsync(cancellationToken);

        var activities = await _dbContext.Set<OldschoolRunescapeActivity>()
            .Where(activity => activity.OldschoolRunescapeUserId == id)
            .Select(activity => new { activity.ActivityId, activity.Name, activity.Score, activity.Rank })
            .ToListAsync(cancellationToken);

        // Keep only ranked kills of known bosses (activities also include clues/minigames).
        var bosses = activities
            .Where(activity => activity.Score > 0 && BossList.Bosses.Contains(activity.Name))
            .OrderByDescending(activity => activity.Score)
            .Select(activity => new PlayerBoss(activity.ActivityId, activity.Name, activity.Score, activity.Rank))
            .ToList();

        return Ok(new PlayerDetail(id, username, skills, xpHistory, bosses));
    }

    [HttpGet("{id:guid}/skill-history")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSkillHistory(Guid id, [FromQuery] string name, CancellationToken cancellationToken)
    {
        var history = await _dbContext.Set<OldschoolRunescapeStatsSnapshot>()
            .Where(snapshot => snapshot.OldschoolRunescapeUserId == id && snapshot.Name == name)
            .OrderBy(snapshot => snapshot.SnapshotDate)
            .Select(snapshot => new XpPoint(snapshot.SnapshotDate, snapshot.Experience, snapshot.Level))
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    [HttpGet("{id:guid}/boss-history")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetBossHistory(Guid id, [FromQuery] string name, CancellationToken cancellationToken)
    {
        var history = await _dbContext.Set<OldschoolRunescapeActivitySnapshot>()
            .Where(snapshot => snapshot.OldschoolRunescapeUserId == id && snapshot.Name == name)
            .OrderBy(snapshot => snapshot.SnapshotDate)
            .Select(snapshot => new BossPoint(snapshot.SnapshotDate, snapshot.Score, snapshot.Rank))
            .ToListAsync(cancellationToken);

        return Ok(history);
    }
}

public record PlayerSummary(
    Guid UserId,
    string Username,
    int TotalLevel,
    long TotalXp,
    int Rank,
    long? WeeklyXpGain);

public record PlayerSkill(uint SkillId, string Name, int Level, long Experience, int Rank);

public record XpPoint(DateTime Date, long Experience, int Level);

public record BossPoint(DateTime Date, int Score, int Rank);

public record PlayerBoss(uint ActivityId, string Name, int Score, int Rank);

public record PlayerDetail(
    Guid UserId,
    string Username,
    IReadOnlyList<PlayerSkill> Skills,
    IReadOnlyList<XpPoint> XpHistory,
    IReadOnlyList<PlayerBoss> Bosses);
