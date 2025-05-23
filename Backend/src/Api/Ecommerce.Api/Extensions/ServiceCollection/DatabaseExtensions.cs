using Ecommerce.Application.Features.Products.Queries.GetProductList;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class DatabaseExtensions
{

    /// <summary>
    /// Configura Entity Framework y servicios relacionados con base de datos
    /// </summary>
    public static IServiceCollection AddCustomDbContext(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddDbContext<EcommerceDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("ConnectionString")
                ?? throw new InvalidOperationException(
                    "La cadena de conexión 'ConnectionString' no está configurada");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Assembly de migraciones
                sqlOptions.MigrationsAssembly(typeof(EcommerceDbContext).Assembly.FullName);

                // Configuración de resilencia
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);

                // Timeout de comandos
                sqlOptions.CommandTimeout(30);
            });

            // Configuración adicional según el entorno
            //var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            //if (isDevelopment)
            //{
            //    options.EnableSensitiveDataLogging();
            //    options.EnableDetailedErrors();
            //}
        });

        return services;
    }

    
}
