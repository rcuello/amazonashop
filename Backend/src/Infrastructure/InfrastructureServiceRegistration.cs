using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Identity;
using Ecommerce.Application.Models.ImageManagment;
using Ecommerce.Application.Models.Token;
using Ecommerce.Application.Persistence;
using Ecommerce.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ecommerce.Infrastructure.Services.Auth;
using Ecommerce.Application.Models.Email;
using Ecommerce.Infrastructure.MessageImplementation;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Persistence;

public static class InfrastructureServiceRegistration
{
    private static IServiceCollection ConfigureEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        var environment = configuration["Environment"] ?? "Development";

        // Determinar qué servicio de correo utilizar según el entorno
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            // Usar Mailtrap para entorno de desarrollo
            services.AddScoped<IEmailService, MailtrapEmailService>();
            services.AddLogging(builder =>
            {
                builder.AddFilter("Ecommerce.Infrastructure.MailtrapEmailService", LogLevel.Debug);
            });
        }
        else
        {
            // Usar SendGrid para entornos de producción y otros
            services.AddScoped<IEmailService, SendgridEmailService>();
            services.AddLogging(builder =>
            {
                builder.AddFilter("Ecommerce.Infrastructure.SendgridEmailService", LogLevel.Information);
            });
        }

        return services;
    }
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)    
    {

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IAsyncRepository<>), typeof(RepositoryBase<>));

        services.AddTransient<IEmailService, SendgridEmailService>();
        services.AddTransient<IAuthService, AuthService>();
        
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));

        services.ConfigureEmailServices(configuration);
        

        return services;
    }
}