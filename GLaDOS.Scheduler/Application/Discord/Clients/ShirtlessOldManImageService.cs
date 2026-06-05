using System.Text.Json.Serialization;

namespace GLaDOS.Scheduler.Application.Discord.Clients;

public class ShirtlessOldManImageService : IShirtlessOldManImageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShirtlessOldManImageService> _logger;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    public ShirtlessOldManImageService(
        HttpClient httpClient,
        ILogger<ShirtlessOldManImageService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ShirtlessOldManImageResult> GetRandomImageUrlAsync(CancellationToken cancellationToken = default)
    {
        var query = Uri.EscapeDataString("shirtless old man");
        var url = $"/search.json?q={query}&restrict_sr=&sort=top&t=all&limit=100";

        try
        {
            _logger.LogInformation("Calling Reddit API: GET {Url}", url);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Reddit API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
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
