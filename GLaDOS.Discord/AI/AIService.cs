using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Glados.Discord.AI;

public class AIService
{
    private readonly IConfiguration _configuration;

    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://opencode.ai/zen/v1/"),
        Timeout = TimeSpan.FromSeconds(120)
    };

    public string? LastError { get; private set; }

    public AIService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string?> SendAsync(
        string systemPrompt,
        string userPrompt,
        string model = "nemotron-3-super-free",
        int maxTokens = 2000,
        double temperature = 0.7,
        CancellationToken ct = default)
    {
        var apiKey = _configuration["OpenCode:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            LastError = "API key not configured (OpenCode:ApiKey missing in config).";
            Console.WriteLine($"AI request skipped: {LastError}");
            return null;
        }

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = maxTokens,
            temperature
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                var truncated = errorBody[..Math.Min(errorBody.Length, 300)];
                LastError = $"API returned {(int)response.StatusCode}: {truncated}";
                Console.WriteLine(LastError);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            LastError = null;
            return content;
        }
        catch (HttpRequestException ex)
        {
            LastError = $"Network error: {ex.Message}";
            Console.WriteLine(LastError);
            return null;
        }
        catch (TaskCanceledException)
        {
            LastError = "Request timed out after 120 seconds.";
            Console.WriteLine(LastError);
            return null;
        }
        catch (Exception ex)
        {
            LastError = $"{ex.GetType().Name}: {ex.Message}";
            Console.WriteLine(LastError);
            return null;
        }
    }
}
