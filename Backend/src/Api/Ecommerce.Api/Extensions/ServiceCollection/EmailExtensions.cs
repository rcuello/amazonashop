using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Models.Email;
using Ecommerce.Infrastructure.MessageImplementation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class EmailExtensions
{
    /// <summary>
    /// Configura el servicio de correo electrónico
    /// </summary>
    public static IServiceCollection AddCustomEmailService(this IServiceCollection services , IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        services.AddTransient<IEmailService, MailtrapEmailService>();
        //services.AddTransient<IEmailService, SendgridEmailService>();

        // Configuración del renderizador de plantillas
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
            // Cargar configuración desde appsettings.json si existe
            configuration.GetSection("TemplateRendererOptions").Bind(options);

            // Valores por defecto específicos o personalizaciones adicionales
            options.BasePath = options.BasePath ?? "_Embedded/EmailTemplates";
            options.FileExtension = options.FileExtension ?? ".cshtml";

            // Ajustar la configuración de caché según el entorno
            var environment = configuration["Environment"] ?? "Development";
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                // En desarrollo: caché más corto para facilitar las pruebas
                options.EnableCaching = true;
                options.CacheDuration = TimeSpan.FromMinutes(1);
            }
            else
            {
                // En producción: caché más largo para mejor rendimiento
                options.EnableCaching = true;
                options.CacheDuration = TimeSpan.FromHours(1);
                options.MaxConcurrentCompilations = 10; // Más concurrencia en producción
            }
        });

        // Registrar caché de memoria compartida para plantillas
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 10_000; // Limitar tamaño de caché
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(30);
        });

        // Registrar el servicio de renderizado como singleton para mantener la caché
        services.TryAddSingleton<ITemplateRender, EmbeddedTemplateRenderer>();

        return services;
    }

    /// <summary>
    /// Método de extensión para precargar las plantillas de correo al iniciar la aplicación
    /// </summary>
    public static IApplicationBuilder UseCustomTemplatePreloading(this IApplicationBuilder app, bool onlyInProduction = true)
    {
        var env = app.ApplicationServices.GetService<IConfiguration>()?["Environment"] ?? "Development";

        // Solo precargar en producción si se especifica
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
            logger?.LogWarning("No se pudo precargar las plantillas: el servicio de renderizado no está disponible o no es compatible");
            return app;
        }

        // Precargar plantillas en segundo plano para no bloquear el arranque
        Task.Run(async () =>
        {
            try
            {
                // Pequeña pausa para permitir que la aplicación termine de iniciar
                await Task.Delay(TimeSpan.FromSeconds(2));

                logger?.LogInformation("Iniciando precarga de plantillas de correo electrónico...");
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
