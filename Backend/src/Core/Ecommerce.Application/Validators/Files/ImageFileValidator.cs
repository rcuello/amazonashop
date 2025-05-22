using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Application.Validators.Files
{
    public class ImageFileValidator : AbstractValidator<IFormFile>
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedContentTypes = {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "image/webp"
        };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public ImageFileValidator()
        {
            // Validación básica
            RuleFor(file => file)
                .NotNull().WithMessage("El archivo no puede ser nulo");

            RuleFor(file => file.Length)
                .GreaterThan(0).WithMessage("El archivo no puede estar vacío")
                .LessThanOrEqualTo(MaxFileSize).WithMessage("El tamaño del archivo no puede exceder los 10MB");

            RuleFor(file => file.FileName)
                .Must(ValidateFileExtension)
                .WithMessage($"La extensión del archivo debe ser alguna de las siguientes: {string.Join(", ", AllowedExtensions)}");

            RuleFor(file => file.ContentType)
                .Must(contentType => AllowedContentTypes.Contains(contentType.ToLower()))
                .WithMessage($"El tipo de archivo debe ser alguno de los siguientes: {string.Join(", ", AllowedContentTypes)}");
        }

        private static bool ValidateFileExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }
    }
}
