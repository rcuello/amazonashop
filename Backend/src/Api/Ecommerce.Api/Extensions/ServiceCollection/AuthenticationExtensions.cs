using Ecommerce.Application.Identity;
using Ecommerce.Application.Models.Token;
using Ecommerce.Domain;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Configura la autenticación JWT y Identity
    /// </summary>
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services,IConfiguration configuration)
    {
        // Servicios de autenticación e identidad
        services.AddCustomAuthenticacionServices(configuration);
        
        // Configurar Identity
        services.AddCustomIdentity();

        // Configurar JWT
        services.AddCustomJwtAuthentication(configuration);


        return services;
    }

    private static IServiceCollection AddCustomAuthenticacionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddTransient<IAuthService, AuthService>();
        
        return services;
    }

    private static IServiceCollection AddCustomIdentity(this IServiceCollection services)
    {
        var identityBuilder = services.AddIdentityCore<Usuario>();
        identityBuilder = new IdentityBuilder(identityBuilder.UserType, identityBuilder.Services);

        identityBuilder
            .AddRoles<IdentityRole>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<UserClaimsPrincipalFactory<Usuario, IdentityRole>>()
            .AddEntityFrameworkStores<EcommerceDbContext>()
            .AddSignInManager<SignInManager<Usuario>>();

        return services;
    }

    private static IServiceCollection AddCustomJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtKey = configuration["JwtSettings:key"]
            ?? throw new InvalidOperationException("JwtSettings:key no configurado");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                // Configuración adicional para debugging
                /*options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("Autenticación JWT falló: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };*/
            });

        return services;
    }   
}
