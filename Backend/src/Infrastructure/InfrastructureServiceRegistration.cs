using Ecommerce.Application.Identity;
using Ecommerce.Application.Models.ImageManagment;
using Ecommerce.Application.Models.Token;
using Ecommerce.Application.Persistence;
using Ecommerce.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ecommerce.Infrastructure.Services.Auth;
using Ecommerce.Application.Models.Payment;

namespace Ecommerce.Infrastructure.Persistence;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddCustomInfrastructureServices(this IServiceCollection services, IConfiguration configuration)    
    {

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IAsyncRepository<>), typeof(RepositoryBase<>));

        // Servicios de autenticación e identidad
       
        services.AddTransient<IAuthService, AuthService>();
        
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // Servicios de almacenamiento de imágenes
        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));

        // Servicios de pagos
        services.Configure<StripeSettings>(configuration.GetSection("StripeSettings"));       
        

        return services;
    }
}