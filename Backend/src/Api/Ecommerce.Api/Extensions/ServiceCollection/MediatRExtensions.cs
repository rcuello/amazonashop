using Ecommerce.Application.Behaviors;
using Ecommerce.Application.Features.Products.Queries.GetProductList;
using MediatR;
using System.Reflection;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class MediatRExtensions
{
    public static IServiceCollection AddCustomMediatR(this IServiceCollection services)
    {
        // Registrar MediatR usando el assembly donde están los handlers
        
        services.AddMediatR(typeof(GetProductListQueryHandler).Assembly);

        // Registrar los comportamientos de MediatR
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
