using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GLaDOS.OsrsWiki.Responses;

public class OsrsWikiSyncResponse
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("quests")]
    public Dictionary<string, int> Quests { get; set; } = new();

    [JsonPropertyName("achievement_diaries")]
    public Dictionary<string, AchievementDiaryRegion> AchievementDiaries { get; set; } = new();

    [JsonPropertyName("levels")]
    public Dictionary<string, int> Levels { get; set; } = new();

    [JsonPropertyName("music_tracks")]
    public Dictionary<string, bool> MusicTracks { get; set; } = new();

    [JsonPropertyName("combat_achievements")]
    public int[] CombatAchievements { get; set; } = Array.Empty<int>();

    [JsonPropertyName("league_tasks")]
    public object[] LeagueTasks { get; set; } = Array.Empty<object>();

    [JsonPropertyName("bingo_tasks")]
    public object[] BingoTasks { get; set; } = Array.Empty<object>();

    [JsonPropertyName("collection_log")]
    public int[] CollectionLog { get; set; } = Array.Empty<int>();

    [JsonPropertyName("collectionLogItemCount")]
    public int? CollectionLogItemCount { get; set; }

    [JsonPropertyName("sea_charting")]
    public int[] SeaCharting { get; set; } = Array.Empty<int>();
}

public class AchievementDiaryRegion
{
    [JsonPropertyName("Easy")]
    public DiaryDifficulty? Easy { get; set; }

    [JsonPropertyName("Medium")]
    public DiaryDifficulty? Medium { get; set; }

    [JsonPropertyName("Hard")]
    public DiaryDifficulty? Hard { get; set; }

    [JsonPropertyName("Elite")]
    public DiaryDifficulty? Elite { get; set; }
}

public class DiaryDifficulty
{
    [JsonPropertyName("complete")]
    public bool Complete { get; set; }

    [JsonPropertyName("tasks")]
    public bool[] Tasks { get; set; } = Array.Empty<bool>();
}