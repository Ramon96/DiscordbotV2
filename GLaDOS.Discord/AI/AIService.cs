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
            Console.WriteLine("AI request skipped: OpenCode:ApiKey not configured.");
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
                Console.WriteLine($"AI request returned {(int)response.StatusCode}: {errorBody[..Math.Min(errorBody.Length, 200)]}");
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"AI request network error: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("AI request timed out.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI request failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }
}
