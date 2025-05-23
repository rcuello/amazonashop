using AutoMapper;
using Ecommerce.Application.Behaviors;
using Ecommerce.Application.Features.Auths.Users.Commands.RegisterUser;
using Ecommerce.Application.Mappings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddCustomApplicationServices(
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


        // Registrar FluentValidation
        services.AddValidatorsFromAssembly(typeof(RegisterUserCommandValidator).Assembly);

        return services;
    }

}