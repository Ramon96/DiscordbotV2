using System.Net.Http.Json;
using GLaDOS.OsrsWiki.Clients.Contracts;
using GLaDOS.OsrsWiki.Responses;

namespace GLaDOS.OsrsWiki.Clients;

public class OsrsWikiItemClient : IOsrsWikiItemClient
{
    private readonly HttpClient _httpClient;

    public OsrsWikiItemClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<OsrsWikiItemInfo?> GetItemDetailsAsync(int itemId, CancellationToken cancellationToken)
    {
        var query = $"bucket('infobox_item').where('item_id','{itemId}').select('item_name','image','item_id','examine','high_alchemy_value','value').run()";
        
        var url = $"api.php?action=bucket&format=json&origin=*&query={Uri.EscapeDataString(query)}";
        
        try 
        {
            var response = await _httpClient.GetFromJsonAsync<OsrsWikiItemResponse>(url, cancellationToken);
            return response?.Items.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
    
    public string? GetImageUrl(string wikiFileName)
    {
        if (string.IsNullOrEmpty(wikiFileName))
        {
            return null;
        }
        var cleanName = wikiFileName.Replace("File:", "").Trim();
        var urlName = cleanName.Replace(" ", "_");

        return $"https://oldschool.runescape.wiki/images/{urlName}";
    }
}