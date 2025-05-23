using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class OpenApiExtensions
{
    /// <summary>
    /// Configura OpenAPI/Swagger para documentación de la API
    /// </summary>
    public static IServiceCollection AddCustomOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<SecuritySchemeTransformer>();
        });

        return services;
    }

    public static WebApplication UseCustomOpenApi(this WebApplication app, bool isDevelopment)
    {
        if (isDevelopment)
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Ecommerce API");
            });

            app.MapGet("/", context =>
            {
                context.Response.Redirect("/swagger");
                return Task.CompletedTask;
            });
        }
        return app;
    }

    /// <summary>
    /// Transformador para agregar esquemas de seguridad
    /// </summary>
    private class SecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info = new OpenApiInfo
            {
                Title = "Ecommerce API",
                Version = "v1",
                Description = "API para aplicación de comercio electrónico",
                Contact = new OpenApiContact
                {
                    Name = "Equipo de Desarrollo",
                    Email = "dev@ecommerce.com"
                }
            };

            // Agregar esquema de seguridad JWT
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Ingrese el token JWT en el formato: Bearer {token}"
            };

            // Aplicar seguridad globalmente
            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            return Task.CompletedTask;
        }
    }
}
