using Ecommerce.Application.Features.Auths.Users.Commands.RegisterUser;
using FluentValidation;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class FluentValidationExtensions
{
    public static IServiceCollection AddCustomFluentValidation(this IServiceCollection services)
    {
        // Registrar FluentValidation
        services.AddValidatorsFromAssembly(typeof(RegisterUserCommandValidator).Assembly);
        return services;
    }
}
