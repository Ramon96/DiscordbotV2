using System.Net.Http.Json;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using GLaDOS.OldschoolRunescape.Responses;
using GLaDOS.OldschoolRunescape.Requests;

namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeClient : IOldschoolRunescapeClient
{
    private readonly HttpClient _httpClient;

    public OldschoolRunescapeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OldschoolRunescapeHiscoreResponse?> GetHiScoresByUsernameAsync(OldschoolRunescapeHiscoreRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        try
        {
            var username = request.Username;
            var formattedUsername = username.ToLower().Replace(' ', '_');

            var url = $"https://secure.runescape.com/m=hiscore_oldschool/index_lite.json?player={Uri.EscapeDataString(formattedUsername)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<OldschoolRunescapeHiscoreResponse>(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new Exception($"Failed to fetch hiscores for {request.Username}", exception);
        }
    }
}