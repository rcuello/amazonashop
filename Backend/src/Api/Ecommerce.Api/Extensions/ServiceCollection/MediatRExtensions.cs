using Ecommerce.Application.Behaviors;
using Ecommerce.Application.Features.Products.Queries.GetProductList;
using MediatR;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class MediatRExtensions
{
    public static IServiceCollection AddCustomMediatR(this IServiceCollection services)
    {
        // Registrar MediatR usando el assembly donde están los handlers
        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssembly(typeof(GetProductListQueryHandler).Assembly);

            // Registrar los comportamientos de MediatR
            options.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)); // 1. Validación
            options.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));// 2. Logging
            options.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>)); // 3. Performance
            options.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));// 4. Excepciones


        });               

        return services;
    }
}
