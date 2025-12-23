using GLaDOS.OsrsWiki.Clients;
using GLaDOS.OsrsWiki.Clients.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace GLaDOS.OsrsWiki.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOsrsWikiServices(this IServiceCollection services)
    {
        services
            .AddHttpClient<IOsrsWikiSyncClient, OsrsWikiSyncClient>(client =>
            {
                client.BaseAddress = new Uri("https://sync.runescape.wiki/runelite/player/");
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");
                
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            });

        services
            .AddHttpClient<IOsrsWikiItemClient, OsrsWikiItemClient>(client =>
            {
                client.BaseAddress = new Uri("https://oldschool.runescape.wiki/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            });
        
        return services;
    }
}