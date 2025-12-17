namespace GLaDOS.OldschoolRunescape.Responses;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class OldschoolRunescapeHiscoreResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("skills")]
    public List<OldschoolRunescapeSkill> Skills { get; set; } = new();

    [JsonPropertyName("activities")]
    public List<OldschoolRunescapeActivity> Activities { get; set; } = new();
}

public class OldschoolRunescapeSkill
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("xp")]
    public long Xp { get; set; }
}

public class OldschoolRunescapeActivity
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }
}