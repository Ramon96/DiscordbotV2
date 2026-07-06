using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.OldschoolRunescape.Responses;

namespace GLaDOS.Scheduler.Extensions;

public class HiscoreCalculator
{
    public OldschoolRunescapeHiscoreChanges CalculateUpdates(OldschoolRunescapeUser user, OldschoolRunescapeHiscoreResponse freshData)
    {
        var changes = new OldschoolRunescapeHiscoreChanges();
        
        foreach (var freshStat in freshData.Skills)
        {
            var existingStat = user.Stats!.FirstOrDefault(s => s.Name == freshStat.Name);
            // Track any XP gain, not just level-ups, so progress is captured for maxed skills too.
            if (existingStat != null &&
                (existingStat.Level != freshStat.Level || existingStat.Experience != freshStat.Xp))
            {
                changes.StatChanges.Add(new StatChange
                {
                    StatName = freshStat.Name,
                    OldLevel = existingStat.Level,
                    NewLevel = freshStat.Level,
                    OldExperience = existingStat.Experience,
                    NewExperience = freshStat.Xp,
                    OldRank = existingStat.Rank,
                    NewRank = freshStat.Rank,
                    StatId = existingStat.SkillId
                });
            }
        }
        
        foreach (var freshActivity in freshData.Activities)
        {
            var existingActivity = user.Activities!.FirstOrDefault(a => a.Name == freshActivity.Name);
            if (existingActivity != null && existingActivity.Score != freshActivity.Score)
            {
                changes.ActivityChanges.Add(new ActivityChange
                {
                    ActivityName = freshActivity.Name,
                    OldScore = existingActivity.Score,
                    NewScore = freshActivity.Score,
                    ScoreDifference = freshActivity.Score - existingActivity.Score,
                    OldRank = existingActivity.Rank,
                    NewRank = freshActivity.Rank
                });
            }
        }
        return changes;
    }
}