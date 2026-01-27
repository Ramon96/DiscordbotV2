using System.Net.Http.Json;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using GLaDOS.OldschoolRunescape.Requests;
using GLaDOS.OldschoolRunescape.Responses;

namespace GLaDOS.OldschoolRunescape.Clients;

public class OldschoolRunescapeClient : IOldschoolRunescapeClient
{
    private readonly HttpClient _httpClient;

    public OldschoolRunescapeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OldschoolRunescapeHiscoreResponse?> GetHiScoresByUsernameAsync(OldschoolRunescapeHiscoreRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var username = request.Username;
            var formattedUsername = username.ToLower().Replace(' ', '_');
            var relativeUrl = $"index_lite.json?player={Uri.EscapeDataString(formattedUsername)}";
            
            var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<OldschoolRunescapeHiscoreResponse>(cancellationToken: cancellationToken) ?? null;
        }
        catch (HttpRequestException exception)
        {
            throw new Exception($"Failed to fetch hiscores for {request.Username}", exception);
        }
    }
}