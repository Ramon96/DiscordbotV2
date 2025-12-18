using GLaDOS.Infra.Repositories;
using GLaDOS.Infra.Repositories.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace GLaDOS.Infra.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfraServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}
