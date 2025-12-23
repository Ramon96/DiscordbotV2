using GLaDOS.OsrsWiki.Responses;

namespace GLaDOS.OsrsWiki.Clients.Contracts;

public interface IOsrsWikiItemClient
{
    Task<OsrsWikiItemInfo?> GetItemDetailsAsync(int itemId, CancellationToken cancellationToken = default);
    string GetImageUrl(string wikiFileName);
}