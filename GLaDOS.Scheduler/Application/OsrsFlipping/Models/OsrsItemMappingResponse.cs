using System.Text.Json.Serialization;

namespace GLaDOS.Scheduler.Application.OsrsFlipping.Models;

public class OsrsMappingEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("examine")]
    public string? Examine { get; set; }

    [JsonPropertyName("members")]
    public bool? Members { get; set; }

    [JsonPropertyName("lowalch")]
    public int? LowAlch { get; set; }

    [JsonPropertyName("highalch")]
    public int? HighAlch { get; set; }

    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}
