using Ecommerce.Application.Persistence;
using Ecommerce.Infrastructure.Persistence.Repositories;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class RepositoryExtensions
{
    public static IServiceCollection AddCustomRepositoryServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IAsyncRepository<>), typeof(RepositoryBase<>));

        return services;
    }
}
