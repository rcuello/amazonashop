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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Builder;
using Ecommerce.Application.Models.Payment;

namespace Ecommerce.Infrastructure.Persistence;

public static class InfrastructureServiceRegistration
{
    private static IServiceCollection ConfigureEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        var environment = configuration["Environment"] ?? "Development";

        // Determinar qu� servicio de correo utilizar seg�n el entorno
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
            // Usar SendGrid para entornos de producci�n y otros
            services.AddScoped<IEmailService, SendgridEmailService>();
            services.AddLogging(builder =>
            {
                builder.AddFilter("Ecommerce.Infrastructure.SendgridEmailService", LogLevel.Information);
            });
        }

        // Configuraci�n del renderizador de plantillas
        ConfigureTemplateRenderer(services, configuration);

        return services;
    }

    /// <summary>
    /// Configura el servicio de renderizado de plantillas
    /// </summary>
    private static IServiceCollection ConfigureTemplateRenderer(this IServiceCollection services, IConfiguration configuration)
    {
        // Registrar y configurar las opciones del renderizador de plantillas
        services.Configure<TemplateRendererOptions>(options =>
        {
            // Cargar configuraci�n desde appsettings.json si existe
            configuration.GetSection("TemplateRendererOptions").Bind(options);

            // Valores por defecto espec�ficos o personalizaciones adicionales
            options.BasePath = options.BasePath ?? "_Embedded/EmailTemplates";
            options.FileExtension = options.FileExtension ?? ".cshtml";

            // Ajustar la configuraci�n de cach� seg�n el entorno
            var environment = configuration["Environment"] ?? "Development";
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                // En desarrollo: cach� m�s corto para facilitar las pruebas
                options.EnableCaching = true;
                options.CacheDuration = TimeSpan.FromMinutes(1);
            }
            else
            {
                // En producci�n: cach� m�s largo para mejor rendimiento
                options.EnableCaching = true;
                options.CacheDuration = TimeSpan.FromHours(1);
                options.MaxConcurrentCompilations = 10; // M�s concurrencia en producci�n
            }
        });

        // Registrar cach� de memoria compartida para plantillas
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 10_000; // Limitar tama�o de cach�
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(30);
        });

        // Registrar el servicio de renderizado como singleton para mantener la cach�
        services.TryAddSingleton<ITemplateRender, EmbeddedTemplateRenderer>();

        return services;
    }
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)    
    {

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IAsyncRepository<>), typeof(RepositoryBase<>));

        // Servicios de autenticaci�n e identidad
        services.AddTransient<IEmailService, SendgridEmailService>();
        services.AddTransient<IAuthService, AuthService>();
        
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // Servicios de almacenamiento de im�genes
        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));

        // Servicios de pagos
        services.Configure<StripeSettings>(configuration.GetSection("StripeSettings"));

        // Servicios de correo y plantillas
        services.ConfigureEmailServices(configuration);
        

        return services;
    }

    /// <summary>
    /// M�todo de extensi�n para precargar las plantillas de correo al iniciar la aplicaci�n
    /// </summary>
    public static IApplicationBuilder UseTemplatePreloading(this IApplicationBuilder app, bool onlyInProduction = true)
    {
        var env = app.ApplicationServices.GetService<IConfiguration>()?["Environment"] ?? "Development";

        // Solo precargar en producci�n si se especifica
        if (onlyInProduction && env.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            return app;
        }

        // Obtener servicios necesarios
        var serviceProvider = app.ApplicationServices;
        var logger = serviceProvider.GetService<ILogger<EmbeddedTemplateRenderer>>();
        var templateRenderer = serviceProvider.GetService<ITemplateRender>() as EmbeddedTemplateRenderer;

        if (templateRenderer == null)
        {
            logger?.LogWarning("No se pudo precargar las plantillas: el servicio de renderizado no est� disponible o no es compatible");
            return app;
        }

        // Precargar plantillas en segundo plano para no bloquear el arranque
        Task.Run(async () =>
        {
            try
            {
                // Peque�a pausa para permitir que la aplicaci�n termine de iniciar
                await Task.Delay(TimeSpan.FromSeconds(2));

                logger?.LogInformation("Iniciando precarga de plantillas de correo electr�nico...");
                await templateRenderer.PrecacheTemplatesAsync();
                logger?.LogInformation("Precarga de plantillas completada exitosamente");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error durante la precarga de plantillas");
            }
        });

        return app;
    }
}