using GLaDOS.Scheduler.Application.OsrsFlipping.Models;

namespace GLaDOS.Scheduler.Application.OsrsFlipping.Clients;

public interface IOsrsPriceClient
{
    Task<OsrsPriceResponse?> GetLatestPricesAsync(CancellationToken cancellationToken = default);
    Task<List<OsrsMappingEntry>> GetItemMappingsAsync(CancellationToken cancellationToken = default);
}
