namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeHiscoreChanges
{
    public List<StatChange> StatChanges { get; set; } = new();
    public List<ActivityChange> ActivityChanges { get; set; } = new();
    public bool HasChanges => StatChanges.Any() || ActivityChanges.Any();
}

public class StatChange
{
    public required string StatName { get; set; }
    public uint StatId { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public long OldExperience { get; set; }
    public long NewExperience { get; set; }
    public int OldRank { get; set; }
    public int NewRank { get; set; }
}

public class ActivityChange
{
    public required string ActivityName { get; set; }
    public int OldScore { get; set; }
    public int NewScore { get; set; }
    public int ScoreDifference { get; set; }
    public int OldRank { get; set; }
    public int NewRank { get; set; }
}