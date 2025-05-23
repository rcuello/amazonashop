namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class CacheExtensions
{
    /// <summary>
    /// Configura Memory Cache con opciones optimizadas
    /// </summary>
    public static IServiceCollection AddCustomCache(this IServiceCollection services)
    {
        // 1. Memory Cache (para datos frecuentemente accedidos y de corta duración)
        services.AddMemoryCache(options =>
        {
            // OPCIÓN A: Sin SizeLimit (no requerirá Size en cada entrada)
            // Sin límite de tamaño para evitar el error de Size requerido
            options.SizeLimit = null; // Comentar o eliminar SizeLimit

            // OPCIÓN B: Con SizeLimit (requerirá Size en cada entrada)
            // Sino es especificado probablemente se lance el error:
            // System.InvalidOperationException: 'Cache entry must specify a value for Size when SizeLimit is set.'
            //options.SizeLimit = 1024; // Límite de entradas en caché                      

            // Configuración de limpieza
            options.CompactionPercentage = 0.25; // 25% de compactación cuando sea necesario
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Escaneo cada 5 minutos
        });

        // Registro de logger específico para caché si es necesario
        services.AddLogging(builder =>
        {
            builder.AddFilter("Microsoft.Extensions.Caching.Memory", LogLevel.Warning);
        });

        return services;
    }

    /// <summary>
    /// Configura Distributed Cache (Redis, SQL Server, etc.) - Preparado para el futuro
    /// </summary>
    public static IServiceCollection AddCustomDistributedCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheProvider = configuration.GetValue<string>("Cache:Provider");

        switch (cacheProvider?.ToLowerInvariant())
        {
            case "redis":
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = configuration.GetConnectionString("Redis");
                    options.InstanceName = configuration.GetValue<string>("Cache:InstanceName") ?? "EcommerceApp";
                });
                break;

            case "sqlserver":
                services.AddDistributedSqlServerCache(options =>
                {
                    options.ConnectionString = configuration.GetConnectionString("ConnectionString");
                    options.SchemaName = "dbo";
                    options.TableName = "AppCache";
                });
                break;

            default:
                // Fallback a In-Memory distributed cache
                services.AddDistributedMemoryCache();
                break;
        }

        return services;
    }
}
