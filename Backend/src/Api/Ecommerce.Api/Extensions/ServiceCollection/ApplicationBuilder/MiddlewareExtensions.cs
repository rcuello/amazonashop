namespace Ecommerce.Api.Extensions.ServiceCollection.ApplicationBuilder;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Configura el pipeline de middlewares en el orden correcto
    /// </summary>
    public static WebApplication UseCustomMiddlewares(this WebApplication app,bool isDevelopment)
    {
        // ===== Uso DE OpenApi =====
        app.UseCustomOpenApi(isDevelopment);

        // 1. HTTPS Redirection (debe ser temprano en el pipeline)
        app.UseHttpsRedirection();

        // 2. Exception Handling (debe ser uno de los primeros)
        app.UseCustomExceptionMiddleware();

        // 3. Rate Limiting (antes de autenticación)
        app.UseCustomRateLimiter();

        // 4. Authentication & Authorization (orden importante)
        app.UseAuthentication();
        app.UseAuthorization();

        // 5. CORS (después de auth para políticas específicas si es necesario)
        app.UseCustomCors();

        // 6. Template Preloading (funcionalidad específica de la aplicación)
        app.UseCustomTemplatePreloading(onlyInProduction: false);

        // 7. Controllers (debe ser al final)       
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Configura middlewares adicionales de monitoreo y observabilidad
    /// </summary>
    public static IApplicationBuilder UseCustomMonitoring(this IApplicationBuilder app)
    {
        // Health checks
        app.UseHealthChecks("/health");

        // Request logging middleware personalizado
        //app.UseRequestResponseLogging();

        // Performance monitoring
        //app.UsePerformanceMonitoring();

        return app;
    }

    /// <summary>
    /// Middleware personalizado para logging de requests/responses
    /// </summary>
    /*private static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<MiddlewareExtensions>>();
            var startTime = DateTime.UtcNow;

            // Log request
            logger.LogInformation("📥 {Method} {Path} from {RemoteIp}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            await next();

            // Log response
            var duration = DateTime.UtcNow - startTime;
            logger.LogInformation("📤 {Method} {Path} responded {StatusCode} in {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                duration.TotalMilliseconds);
        });
    }
    */
    /// <summary>
    /// Middleware personalizado para monitoreo de rendimiento
    /// </summary>
    /*private static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<MiddlewareExtensions>>();
            var startTime = DateTime.UtcNow;

            await next();

            var duration = DateTime.UtcNow - startTime;

            // Log requests lentos
            if (duration.TotalMilliseconds > 1000) // Mayor a 1 segundo
            {
                logger.LogWarning("🐌 Slow request detected: {Method} {Path} took {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    duration.TotalMilliseconds);
            }
        });
    }*/
}
