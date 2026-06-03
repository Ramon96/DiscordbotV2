using GLaDOS.Domain;

namespace GLaDOS.Domain.OsrsFlipping;

public class OsrsPriceSnapshot : Entity
{
    public int OsrsItemId { get; init; }
    public long AvgBuyPrice { get; set; }
    public long AvgSellPrice { get; set; }
    public long Volume { get; set; }
    public DateTime Timestamp { get; set; }
}
