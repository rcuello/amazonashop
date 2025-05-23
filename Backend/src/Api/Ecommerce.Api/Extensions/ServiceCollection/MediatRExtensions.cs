using Ecommerce.Application.Features.Products.Queries.GetProductList;
using MediatR;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class MediatRExtensions
{
    public static IServiceCollection AddCustomMediatR(this IServiceCollection services)
    {
        // Registrar MediatR usando el assembly donde están los handlers        
        services.AddMediatR(typeof(GetProductListQueryHandler).Assembly);

        return services;
    }
}
