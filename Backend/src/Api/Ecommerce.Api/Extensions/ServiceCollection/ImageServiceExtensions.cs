using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Models.ImageManagment;
using Ecommerce.Infrastructure.ImageCloudinary;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class ImageServiceExtensions
{
    /// <summary>
    /// Configura el servicio de gestión de imágenes
    /// </summary>
    public static IServiceCollection AddCustomImageService(this IServiceCollection services, IConfiguration configuration)
    {
        // Servicios de almacenamiento de imágenes
        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
        // Para la subida de imagenes
        services.AddScoped<IManageImageService, CloudinaryManageImageService>();

        return services;
    }
}
