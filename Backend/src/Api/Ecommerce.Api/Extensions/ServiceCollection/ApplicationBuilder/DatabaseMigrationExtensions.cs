using Ecommerce.Domain;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Extensions.ServiceCollection.ApplicationBuilder;

public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Ejecuta migraciones y seed de datos de forma segura
    /// </summary>
    public static async Task<IApplicationBuilder> UseCustomMigrationsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();


        try
        {
            logger.LogInformation("🗃️ Iniciando configuración de base de datos...");

            var context = services.GetRequiredService<EcommerceDbContext>();
            var userManager = services.GetRequiredService<UserManager<Usuario>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();

            // Opción 1: Usar migraciones (recomendado para producción)
            // RC: Por ahora Esto creará la base de datos según el modelo actual, pero no mantendrá las migraciones.
            // await context.Database.MigrateAsync();

            // Opción 2: EnsureCreated (para desarrollo rápido)
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("✅ Base de datos configurada correctamente");

            // Seed de datos
            logger.LogDebug("🌱 Cargando datos iniciales...");
            await EcommerceDbContextData.LoadDataAsync(context, userManager, roleManager, loggerFactory);
            logger.LogInformation("✅ Datos iniciales cargados correctamente");

            logger.LogInformation("🎉 Configuración de base de datos completada exitosamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error durante la configuración de base de datos");
       
            throw;
        }

        return app;
    }

    /// <summary>
    /// Ejecuta migraciones pendientes si las hay
    /// </summary>
    public static async Task<IApplicationBuilder> UseCustomMigrationCheckAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<EcommerceDbContext>();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                logger.LogInformation("📋 Migraciones pendientes encontradas: {Count}", pendingMigrations.Count());
                foreach (var migration in pendingMigrations)
                {
                    logger.LogInformation("  - {Migration}", migration);
                }

                logger.LogInformation("🔄 Aplicando migraciones...");
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Migraciones aplicadas exitosamente");
            }
            else
            {
                logger.LogInformation("✅ Base de datos actualizada - No hay migraciones pendientes");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error al verificar/aplicar migraciones");
            throw;
        }

        return app;
    }

    /// <summary>
    /// Valida la conexión a base de datos
    /// </summary>
    public static async Task<IApplicationBuilder> UseCustomDatabaseHealthCheckAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<EcommerceDbContext>();

            logger.LogInformation("🔍 Verificando conexión a base de datos...");
            var canConnect = await context.Database.CanConnectAsync();

            if (canConnect)
            {
                logger.LogInformation("✅ Conexión a base de datos establecida correctamente");
            }
            else
            {
                logger.LogError("❌ No se pudo establecer conexión a la base de datos");
                throw new InvalidOperationException("No se pudo conectar a la base de datos");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error al verificar la conexión a base de datos");
            throw;
        }

        return app;
    }
}
