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

    private static readonly HashSet<string> SeenUrls = new();
    private static readonly object SeenLock = new();

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
        var url = $"/services/feeds/photos_public.gne?format=json&nojsoncallback=1&safe_search=0&per_page=100&tags={query}&tagmode=any";

        try
        {
            _logger.LogInformation("Calling Flickr API: GET {Url}", url);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Flickr API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
                return new ShirtlessOldManImageResult(null, $"Flickr API returned {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var feed = await response.Content.ReadFromJsonAsync<FlickrFeed>(cancellationToken: cancellationToken);

            var imageUrls = feed?.Items
                ?.Where(i => !string.IsNullOrWhiteSpace(i?.Media?.M))
                .Select(i => i!.Media!.M)
                .Where(u => ImageExtensions.Contains(Path.GetExtension(new Uri(u).AbsolutePath)))
                .ToList();

            if (imageUrls == null || imageUrls.Count == 0)
            {
                _logger.LogWarning("Flickr returned 200 but no image posts found");
                return new ShirtlessOldManImageResult(null, "No image posts found from Flickr search");
            }

            List<string> unseen;
            lock (SeenLock)
            {
                unseen = imageUrls.Where(u => SeenUrls.Add(u)).ToList();
                if (unseen.Count == 0)
                {
                    _logger.LogInformation("All {Count} images already seen, resetting cycle", imageUrls.Count);
                    SeenUrls.Clear();
                    foreach (var u in imageUrls) SeenUrls.Add(u);
                    unseen = imageUrls;
                }
            }

            var selected = unseen[Random.Shared.Next(unseen.Count)];
            _logger.LogInformation("Selected image (unseen: {Unseen}/{Total}): {Url}", unseen.Count, imageUrls.Count, selected);
            return new ShirtlessOldManImageResult(selected, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Flickr API: {Message}", ex.Message);
            return new ShirtlessOldManImageResult(null, $"HTTP error: {ex.Message} (Status: {ex.StatusCode})");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Flickr API request timed out");
            return new ShirtlessOldManImageResult(null, $"Request timed out after {_httpClient.Timeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Flickr API: {Message}", ex.Message);
            return new ShirtlessOldManImageResult(null, $"Unexpected error: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private sealed class FlickrFeed
    {
        [JsonPropertyName("items")]
        public List<FlickrItem>? Items { get; init; }
    }

    private sealed class FlickrItem
    {
        [JsonPropertyName("media")]
        public FlickrMedia? Media { get; init; }
    }

    private sealed class FlickrMedia
    {
        [JsonPropertyName("m")]
        public string M { get; init; } = string.Empty;
    }
}
