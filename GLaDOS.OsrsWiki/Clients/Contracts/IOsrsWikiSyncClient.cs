using GLaDOS.OsrsWiki.Requests;
using GLaDOS.OsrsWiki.Responses;

namespace GLaDOS.OsrsWiki.Clients.Contracts;

public interface IOsrsWikiSyncClient
{
    Task<OsrsWikiSyncResponse?> GetOsrsWikiSyncDataAsync(OsrsWikiSyncRequest request, CancellationToken cancellationToken = default);
}