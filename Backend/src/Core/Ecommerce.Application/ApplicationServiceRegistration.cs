using AutoMapper;
using Ecommerce.Application.Behaviors;
using Ecommerce.Application.Configuration;
using Ecommerce.Application.Features.Auths.Users.Commands.RegisterUser;
using Ecommerce.Application.Mappings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
                        this IServiceCollection services,
                        IConfiguration configuration
    )
    {
        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new MappingProfile());
        });

        IMapper mapper = mapperConfig.CreateMapper();
        services.AddSingleton(mapper);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        //RateLimit
        services.AddRateLimitServices(configuration);

        // Registrar FluentValidation
        services.AddValidatorsFromAssembly(typeof(RegisterUserCommandValidator).Assembly);

        return services;
    }

    private static IServiceCollection AddRateLimitServices(
                        this IServiceCollection services,
                        IConfiguration configuration
    )
    {
        // Configuración de RateLimit
        services.Configure<RateLimitConfiguration>(configuration.GetSection(RateLimitConfiguration.SectionName));

        // Validación de la configuración al startup
        services.AddOptions<RateLimitConfiguration>()
        .Bind(configuration.GetSection(RateLimitConfiguration.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RateLimitingBehavior<,>));

        return services;
    }
}