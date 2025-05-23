using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Infrastructure.ImageCloudinary;
using Microsoft.AspNetCore.Http.Features;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class FileUploadExtensions
{
    /// <summary>
    /// Configura opciones para subida de archivos
    /// </summary>
    public static IServiceCollection AddCustomFileUpload(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.Configure<FormOptions>(options =>
        {
            // Configuración por defecto: 50MB
            var maxFileSize = configuration?.GetValue<long>("FileUpload:MaxSizeBytes") ?? 52428800; // 50MB en bytes

            options.ValueLengthLimit = (int)maxFileSize;
            options.MultipartBodyLengthLimit = maxFileSize;
            options.MultipartHeadersLengthLimit = (int)maxFileSize;
            options.KeyLengthLimit = 2048;
            options.ValueCountLimit = 1024;
        });

        // Registrar servicio de manejo de imágenes
        services.AddScoped<IManageImageService, CloudinaryManageImageService>();

        return services;
    }
}
