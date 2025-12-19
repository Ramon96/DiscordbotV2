namespace GLaDOS.OldschoolRunescape.Responses;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class OldschoolRunescapeHiscoreResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("skills")]
    public ICollection<OldschoolRunescapeSkill> Skills { get; set; }

    [JsonPropertyName("activities")]
    public ICollection<OldschoolRunescapeActivity> Activities { get; set; }
}

public class OldschoolRunescapeSkill
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("xp")]
    public ulong Xp { get; set; }
}

public class OldschoolRunescapeActivity
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }
}