using System.Text.Json.Serialization;

namespace GLaDOS.Scheduler.Application.OsrsFlipping.Models;

public class OsrsPriceResponse
{
    [JsonPropertyName("data")]
    public Dictionary<string, OsrsPriceItem> Data { get; set; } = new();
}

public class OsrsPriceItem
{
    [JsonPropertyName("avgHighPrice")]
    public int? AvgHighPrice { get; set; }

    [JsonPropertyName("avgLowPrice")]
    public int? AvgLowPrice { get; set; }

    [JsonPropertyName("highPriceVolume")]
    public long HighPriceVolume { get; set; }

    [JsonPropertyName("lowPriceVolume")]
    public long LowPriceVolume { get; set; }
}
