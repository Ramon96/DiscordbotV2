using GLaDOS.Domain;

namespace GLaDOS.Domain.OsrsFlipping;

public class OsrsItemMapping : Entity
{
    public int OsrsItemId { get; init; }
    public required string Name { get; init; }
    public int? GeLimit { get; set; }
}
