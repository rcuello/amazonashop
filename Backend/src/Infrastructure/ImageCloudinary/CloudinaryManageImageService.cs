using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Models.ImageManagment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Ecommerce.Infrastructure.ImageCloudinary
{
    public class CloudinaryManageImageService : IManageImageService
    {
        public CloudinarySettings _cloudinarySettings { get; }
        private readonly ILogger<CloudinaryManageImageService> _logger;
        private readonly Cloudinary _cloudinary;
        private bool _disposed;

        public CloudinaryManageImageService(IOptions<CloudinarySettings> cloudinarySettings, ILogger<CloudinaryManageImageService> logger)
        {
            _cloudinarySettings = cloudinarySettings?.Value ?? throw new ArgumentNullException(nameof(cloudinarySettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ValidateConfiguration();

            var account = new Account(
                _cloudinarySettings.CloudName,
                _cloudinarySettings.ApiKey,
                _cloudinarySettings.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<ImageResponse> UploadImage(ImageData imageData)
        {            
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            ValidateImageData(imageData);

            try
            {
                _logger.LogInformation("Iniciando subida de imagen: {ImageName}", imageData.Nombre);

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(imageData.Nombre, imageData.ImageStream)                    
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == HttpStatusCode.OK)
                {
                    _logger.LogInformation("Imagen subida exitosamente. PublicId: {PublicId}", uploadResult.PublicId);

                    return new ImageResponse
                    {
                        //Url = uploadResult.Url.ToString(),
                        Url = uploadResult.SecureUrl.ToString(),
                        PublicId = uploadResult.PublicId
                    };
                }

                var errorMessage = $"Error al subir imagen. StatusCode: {uploadResult.StatusCode}, Error: {uploadResult.Error?.Message}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);

            }
            catch (Exception ex) when (!(ex is ArgumentNullException || ex is ArgumentException))
            {
                _logger.LogError(ex, "Error inesperado al subir imagen: {ImageName}", imageData.Nombre);
                throw new InvalidOperationException("No se pudo guardar la imagen en el servidor", ex);
            }            

        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_cloudinarySettings.CloudName))
                throw new InvalidOperationException("CloudName no puede estar vacío");

            if (string.IsNullOrWhiteSpace(_cloudinarySettings.ApiKey))
                throw new InvalidOperationException("ApiKey no puede estar vacío");

            if (string.IsNullOrWhiteSpace(_cloudinarySettings.ApiSecret))
                throw new InvalidOperationException("ApiSecret no puede estar vacío");
        }

        private static void ValidateImageData(ImageData imageData)
        {
            if (string.IsNullOrWhiteSpace(imageData.Nombre))
                throw new ArgumentException("El nombre de la imagen no puede estar vacío", nameof(imageData));

            if (imageData.ImageStream == null)
                throw new ArgumentException("El stream de la imagen no puede ser nulo", nameof(imageData));

            if (!imageData.ImageStream.CanRead)
                throw new ArgumentException("El stream de la imagen debe ser legible", nameof(imageData));
        }

    }
}