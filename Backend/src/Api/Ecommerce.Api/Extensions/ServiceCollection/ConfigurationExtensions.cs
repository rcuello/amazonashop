namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Agrega archivos de configuración adicionales
    /// </summary>
    public static IConfigurationBuilder AddCustomConfigurationFiles(
        this IConfigurationBuilder builder,
        IHostEnvironment environment)
    {
        // Agregar archivos de Rate Limiting
        builder
            .AddJsonFile("ratelimiting.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"ratelimiting.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true);


        return builder;
    }
}
