using Ecommerce.Application.Models.ImageManagment;

namespace Ecommerce.Application.Contracts.Infrastructure;
public interface IManageImageService
{
    Task<ImageResponse> UploadImage(ImageData imageStream);
    
}