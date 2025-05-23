namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class CorsExtensions
{
    /// <summary>
    /// Configura políticas CORS para la aplicación
    /// </summary>
    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            // Política permisiva para desarrollo
            options.AddPolicy("AllowAllPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });

            // Política restrictiva para producción
            options.AddPolicy("ProductionPolicy", builder =>
            {
                builder.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });

            // Política específica para APIs
            options.AddPolicy("ApiPolicy", builder =>
            {
                builder.WithOrigins("https://api.yourdomain.com")
                       .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                       .WithHeaders("Content-Type", "Authorization")
                       .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }

    public static WebApplication UseCustomCors(this WebApplication app)
    {
        app.UseCors("AllowAllPolicy");

        return app;
    }


}
