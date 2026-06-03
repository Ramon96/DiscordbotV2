namespace GLaDOS.Domain.OsrsFlipping;

public class FlippingOpportunityDto
{
    public int OsrsItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? GeLimit { get; set; }
    public long AvgBuyPrice { get; set; }
    public long AvgSellPrice { get; set; }
    public long GrossMargin { get; set; }
    public long Tax { get; set; }
    public long NetProfit { get; set; }
    public long Volume { get; set; }
    public DateTime LastUpdated { get; set; }
}
