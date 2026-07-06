namespace GLaDOS.Infra.EntityFramework.Logging;

public class LogEntry
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? SourceContext { get; set; }
    public string? Properties { get; set; }
}
