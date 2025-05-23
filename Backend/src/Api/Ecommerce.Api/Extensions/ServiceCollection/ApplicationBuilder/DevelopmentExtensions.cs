namespace Ecommerce.Api.Extensions.ServiceCollection.ApplicationBuilder;

public static class DevelopmentExtensions
{
    /// <summary>
    /// Configura herramientas y funcionalidades específicas para desarrollo
    /// </summary>
    public static WebApplication UseCustomDevelopment(this WebApplication app ,bool isDevelopmentOrLocal)
    {
        //var environment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        if (!isDevelopmentOrLocal)
            return app;

        // OpenAPI/Swagger
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Ecommerce API");
            options.RoutePrefix = "swagger";

            // Configuraciones adicionales para desarrollo
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
            options.EnableValidator();
            options.ShowExtensions();
        });

        // Redirección automática a Swagger en desarrollo
        app.MapGet("/", context =>
        {
            context.Response.Redirect("/swagger");
            return Task.CompletedTask;
        });

        // Logging adicional para desarrollo        
        logger.LogInformation("🚀 Aplicación ejecutándose en modo desarrollo");
        logger.LogInformation("📖 Documentación disponible en: /swagger");

        return app;
    }

    /// <summary>
    /// Configura herramientas de desarrollo específicas para depuración
    /// </summary>
    public static IApplicationBuilder UseCustomDevelopmentDebugging(this IApplicationBuilder app)
    {
        var environment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

        if (!environment.IsDevelopmentOrLocal())
            return app;

        // Habilitar páginas de error detalladas
        app.UseDeveloperExceptionPage();

        // Logging de requests y responses (opcional)
        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            logger.LogDebug("🔍 Request: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await next();

            logger.LogDebug("✅ Response: {StatusCode} for {Method} {Path}",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path);
        });

        return app;
    }
}