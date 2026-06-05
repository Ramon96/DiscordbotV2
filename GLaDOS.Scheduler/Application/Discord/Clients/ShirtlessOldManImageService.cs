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

    public async Task<ShirtlessOldManImageResult> GetRandomImageUrlAsync(CancellationToken cancellationToken = default)
    {
        var accessKey = _configuration["Unsplash:AccessKey"];
        if (string.IsNullOrWhiteSpace(accessKey))
        {
            _logger.LogWarning("Unsplash API key not configured (Unsplash:AccessKey is missing)");
            return new ShirtlessOldManImageResult(null, "Unsplash:AccessKey not configured in app settings");
        }

        var query = Uri.EscapeDataString("shirtless old man");
        var url = $"/photos/random?query={query}&client_id={accessKey}";

        try
        {
            _logger.LogInformation("Calling Unsplash API: GET {Url}", url);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Unsplash API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
                return new ShirtlessOldManImageResult(null, $"Unsplash API returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }

            var photo = await response.Content.ReadFromJsonAsync<UnsplashPhoto>(cancellationToken: cancellationToken);

            if (photo?.Urls?.Regular == null)
            {
                _logger.LogError("Unsplash API returned 200 but no image URL in response");
                return new ShirtlessOldManImageResult(null, "Unsplash API returned no image URL in response");
            }

            _logger.LogInformation("Got image URL from Unsplash: {Url}", photo.Urls.Regular);
            return new ShirtlessOldManImageResult(photo.Urls.Regular, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Unsplash API: {Message}", ex.Message);
            return new ShirtlessOldManImageResult(null, $"HTTP error: {ex.Message} (Status: {ex.StatusCode})");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Unsplash API request timed out");
            return new ShirtlessOldManImageResult(null, $"Request timed out after {_httpClient.Timeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Unsplash API: {Message}", ex.Message);
            return new ShirtlessOldManImageResult(null, $"Unexpected error: {ex.GetType().Name} - {ex.Message}");
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
