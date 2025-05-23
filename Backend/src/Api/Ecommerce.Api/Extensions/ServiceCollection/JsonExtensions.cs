using System.Text.Json.Serialization;
using System.Text.Json;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class JsonExtensions
{
    /// <summary>
    /// Configura la serialización JSON para toda la aplicación
    /// </summary>
    public static IServiceCollection AddCustomJson(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        var isDevelopment = environment.IsDevelopmentOrLocal();

        // Configurar JSON para HTTP
        services.ConfigureHttpJsonOptions(options =>
        {
            ConfigureJsonOptions(options.SerializerOptions, isDevelopment);
        });

        // Configurar JSON para MVC
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        {
            ConfigureJsonOptions(options.JsonSerializerOptions, isDevelopment);
        });

        // Configurar JSON para Controllers con referencia circular
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                ConfigureJsonOptions(options.JsonSerializerOptions, isDevelopment);
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        // Registrar JsonSerializerOptions como servicio singleton
        services.AddSingleton(serviceProvider =>
        {
            var options = new JsonSerializerOptions();
            ConfigureJsonOptions(options, isDevelopment);
            return options;
        });

        return services;
    }

    private static void ConfigureJsonOptions(JsonSerializerOptions options, bool isDevelopment)
    {
        // Configuración básica
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = isDevelopment;

        // Configuración adicional
        options.PropertyNameCaseInsensitive = true;
        options.AllowTrailingCommas = true;
        options.ReadCommentHandling = JsonCommentHandling.Skip;

        // Manejo de enums
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        // Configuración para fechas (opcional)
        // options.Converters.Add(new JsonDateTimeConverter());
    }
}
