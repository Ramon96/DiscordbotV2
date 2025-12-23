using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.OldschoolRunescape.Responses;

namespace GLaDOS.Scheduler.Extensions.OldschoolRunescape;

public static class HiscoreMappingExtensions
{
    public static OldschoolRunescapeStat ToEntity(this OldschoolRunescapeSkillResponse skill, Guid userId)
    {
        return new OldschoolRunescapeStat
        {
            SkillId = skill.Id,
            Name = skill.Name,
            Level = skill.Level,
            Experience = skill.Xp,
            Rank = skill.Rank,
            OldschoolRunescapeUserId = userId
        };
    }

    public static OldschoolRunescapeActivity ToEntity(this OldschoolRunescapeActivityResponse skill, Guid userId)
    {
        return new OldschoolRunescapeActivity
        {
            ActivityId = skill.Id,
            Name = skill.Name,
            Score = skill.Score,
            Rank = skill.Rank,
            OldschoolRunescapeUserId = userId
        };
    }
}