using System.Text.Json.Serialization;

namespace GLaDOS.Scheduler.Application.Discord.Clients;

public class ShirtlessOldManImageService : IShirtlessOldManImageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShirtlessOldManImageService> _logger;
    private readonly IConfiguration _configuration;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public ShirtlessOldManImageService(
        HttpClient httpClient,
        ILogger<ShirtlessOldManImageService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            return _accessToken;

        var clientId = _configuration["Reddit:ClientId"];
        var clientSecret = _configuration["Reddit:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogWarning("Reddit API credentials not configured");
            return null;
        }

        try
        {
            using var authClient = new HttpClient();
            var auth = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            authClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {auth}");
            authClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "DiscordBot:glados:v1.0.0 (by /u/Ramon96)");

            var content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            ]);

            var response = await authClient.PostAsync("https://www.reddit.com/api/v1/access_token", content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RedditTokenResponse>(cancellationToken: ct);
            if (result?.AccessToken == null)
            {
                _logger.LogError("Reddit OAuth returned no access token");
                return null;
            }

            _accessToken = result.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 60);
            _logger.LogInformation("Got Reddit access token, expires in {Seconds}s", result.ExpiresIn);
            return _accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Reddit OAuth token");
            return null;
        }
    }

    public async Task<ShirtlessOldManImageResult> GetRandomImageUrlAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (token == null)
            return new ShirtlessOldManImageResult(null, "Reddit API credentials not configured or OAuth failed");

        var query = Uri.EscapeDataString("shirtless old man");
        var url = $"/search.json?q={query}&restrict_sr=&sort=top&t=all&limit=100";

        try
        {
            _logger.LogInformation("Calling Reddit API: GET {Url}", url);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Reddit API returned {StatusCode}: {Body}", (int)response.StatusCode, body);

                if ((int)response.StatusCode == 401)
                {
                    _accessToken = null;
                    _tokenExpiry = DateTime.MinValue;
                }

                return new ShirtlessOldManImageResult(null, $"Reddit API returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }

            var listing = await response.Content.ReadFromJsonAsync<RedditListing>(cancellationToken: cancellationToken);

            var imageUrls = listing?.Data?.Children
                ?.Select(c => c?.Data)
                ?.Where(d => d is { PostHint: "image", Url: not null, IsVideo: false })
                ?.Select(d => d.Url!)
                ?.Where(url => ImageExtensions.Contains(Path.GetExtension(new Uri(url).AbsolutePath)))
                ?.ToList();

            if (imageUrls == null || imageUrls.Count == 0)
            {
                _logger.LogError("Reddit API returned 200 but no image posts found");
                return new ShirtlessOldManImageResult(null, "No image posts found from Reddit search");
            }

            var selected = imageUrls[Random.Shared.Next(imageUrls.Count)];
            _logger.LogInformation("Got image URL from Reddit: {Url}", selected);
            return new ShirtlessOldManImageResult(selected, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Reddit API: {Message}", ex.Message);
            return new ShirtlessOldManImageResult(null, $"HTTP error: {ex.Message} (Status: {ex.StatusCode})");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Reddit API request timed out");
            return new ShirtlessOldManImageResult(null, $"Request timed out after {_httpClient.Timeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Reddit API: {Message}", ex.Message);
            return new ShirtlessOldManImageResult(null, $"Unexpected error: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private sealed class RedditTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }

    private sealed class RedditListing
    {
        [JsonPropertyName("data")]
        public RedditListingData? Data { get; init; }
    }

    private sealed class RedditListingData
    {
        [JsonPropertyName("children")]
        public List<RedditChild>? Children { get; init; }
    }

    private sealed class RedditChild
    {
        [JsonPropertyName("data")]
        public RedditPostData? Data { get; init; }
    }

    private sealed class RedditPostData
    {
        [JsonPropertyName("post_hint")]
        public string? PostHint { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("is_video")]
        public bool IsVideo { get; init; }
    }
}
