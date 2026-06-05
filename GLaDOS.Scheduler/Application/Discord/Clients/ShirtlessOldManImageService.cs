using System.Text.Json.Serialization;

namespace GLaDOS.Scheduler.Application.Discord.Clients;

public class ShirtlessOldManImageService : IShirtlessOldManImageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShirtlessOldManImageService> _logger;
    private readonly IConfiguration _configuration;

    public ShirtlessOldManImageService(
        HttpClient httpClient,
        ILogger<ShirtlessOldManImageService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string?> GetRandomImageUrlAsync(CancellationToken cancellationToken = default)
    {
        var accessKey = _configuration["Unsplash:AccessKey"];
        if (string.IsNullOrWhiteSpace(accessKey))
        {
            _logger.LogWarning("Unsplash API key not configured");
            return null;
        }

        var query = Uri.EscapeDataString("shirtless old man");
        var url = $"/photos/random?query={query}&client_id={accessKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var photo = await response.Content.ReadFromJsonAsync<UnsplashPhoto>(cancellationToken: cancellationToken);
            return photo?.Urls.Regular;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch image from Unsplash API");
            return null;
        }
    }

    private sealed class UnsplashPhoto
    {
        [JsonPropertyName("urls")]
        public UnsplashUrls Urls { get; init; } = new();
    }

    private sealed class UnsplashUrls
    {
        [JsonPropertyName("regular")]
        public string Regular { get; init; } = string.Empty;
    }
}
