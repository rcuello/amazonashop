using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Models.ImageManagment;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.ImageCloudinary
{
    public class ManageImageService : IManageImageService
    {
        public CloudinarySettings _cloudinarySettings { get; }

        public ManageImageService(IOptions<CloudinarySettings> cloudinarySettings)
        {
            _cloudinarySettings = cloudinarySettings.Value;
        }
        public async Task<ImageResponse> UploadImage(ImageData imageStream)
        {
            var account = new Account(
                _cloudinarySettings.CloudName,
                _cloudinarySettings.ApiKey,
                _cloudinarySettings.ApiSecret
            );

            var cloudinary = new Cloudinary(account);

            var uploadImage = new ImageUploadParams
            {
                File = new FileDescription(imageStream.Nombre, imageStream.ImageStream)
            };

            var uploadResult = await cloudinary.UploadAsync(uploadImage);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return new ImageResponse
                {
                    Url = uploadResult.Url.ToString(),
                    PublicId = uploadResult.PublicId
                };

            }

            throw new Exception("No se pudo guardar la imagen en el servidor");

        }
    }
}