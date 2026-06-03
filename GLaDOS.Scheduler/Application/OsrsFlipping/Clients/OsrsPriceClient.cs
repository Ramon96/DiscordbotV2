using System.Net.Http.Json;
using System.Text.Json;
using GLaDOS.Scheduler.Application.OsrsFlipping.Models;

namespace GLaDOS.Scheduler.Application.OsrsFlipping.Clients;

public class OsrsPriceClient : IOsrsPriceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OsrsPriceClient> _logger;

    public OsrsPriceClient(HttpClient httpClient, ILogger<OsrsPriceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OsrsPriceResponse?> GetLatestPricesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/v1/osrs/5m", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var priceResponse = JsonSerializer.Deserialize<OsrsPriceResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return priceResponse;
    }

    public async Task<List<OsrsMappingEntry>> GetItemMappingsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/v1/osrs/mapping", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var mappings = JsonSerializer.Deserialize<List<OsrsMappingEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return mappings ?? [];
    }
}
