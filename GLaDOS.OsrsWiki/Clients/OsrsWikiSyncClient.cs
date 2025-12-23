using System.Net.Http.Json;
using GLaDOS.OsrsWiki.Clients.Contracts;
using GLaDOS.OsrsWiki.Requests;
using GLaDOS.OsrsWiki.Responses;

namespace GLaDOS.OsrsWiki.Clients;

public class OsrsWikiSyncClient : IOsrsWikiSyncClient
{
    private readonly HttpClient _httpClient;

    public OsrsWikiSyncClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OsrsWikiSyncResponse?> GetOsrsWikiSyncDataAsync(OsrsWikiSyncRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var username = request.Username;
            var formattedUsername = username.ToLower().Replace(' ', '_');
            
            var relativeUrl = $"{Uri.EscapeDataString(formattedUsername)}/STANDARD";
            
            return await _httpClient.GetFromJsonAsync<OsrsWikiSyncResponse>(relativeUrl, cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}