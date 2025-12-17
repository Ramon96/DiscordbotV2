using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace GLaDOS.OldschoolRunescape.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddOldschoolRunescapeServices(this IServiceCollection services)
    {
        return services
            .AddHttpClient<IOldschoolRunescapeClient, OldschoolRunescapeClient>(client =>
            {
                client.BaseAddress = new Uri("https://secure.runescape.com/m=hiscore_oldschool/");
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
    }
}