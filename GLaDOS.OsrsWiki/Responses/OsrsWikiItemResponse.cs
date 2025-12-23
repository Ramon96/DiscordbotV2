using System.Text.Json.Serialization;

namespace GLaDOS.OsrsWiki.Responses;

public class OsrsWikiItemResponse
{
    [JsonPropertyName("bucket")]
    public List<OsrsWikiItemInfo> Items { get; set; } = new();
}

public class OsrsWikiItemInfo
{
    [JsonPropertyName("item_name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("item_id")]
    public List<string> ItemIds { get; set; } = new();

    [JsonPropertyName("examine")]
    public string Examine { get; set; } = string.Empty;
    
    [JsonPropertyName("image")]
    public List<string> Images { get; set; } = new(); 

    [JsonPropertyName("high_alchemy_value")]
    public uint? HighAlch { get; set; }
    
    [JsonPropertyName("value")]
    public uint? Value { get; set; }
}
