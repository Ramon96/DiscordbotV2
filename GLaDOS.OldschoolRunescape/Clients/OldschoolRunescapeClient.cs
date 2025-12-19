using System.Net.Http.Json;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using GLaDOS.OldschoolRunescape.Responses;

namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeClient : IOldschoolRunescapeClient
{
    private readonly HttpClient _httpClient;

    public OldschoolRunescapeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OldschoolRunescapeHiscoreResponse> GetHiScoresByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://secure.runescape.com/m=hiscore_oldschool/index_lite.json?player={Uri.EscapeDataString(username)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<OldschoolRunescapeHiscoreResponse>(cancellationToken);

            if (content == null)
            {
                throw new Exception("Failed to deserialize hiscore response.");
            }

            return content;
        }
        catch (HttpRequestException exception)
        {
            throw new Exception($"Failed to fetch hiscores for {username}", exception);
        }
    }
}