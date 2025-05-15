using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Application.Validators.Files
{
    public class ImageFileValidator : AbstractValidator<IFormFile>
    {
        private readonly string[] _allowedExtensions;
        private readonly string[] _allowedContentTypes;

        public ImageFileValidator(string[] allowedExtensions, string[] allowedContentTypes)
        {
            _allowedExtensions = allowedExtensions;
            _allowedContentTypes = allowedContentTypes;

            // Validación básica
            RuleFor(file => file)
                .NotNull().WithMessage("El archivo no puede ser nulo");

            RuleFor(file => file.Length)
                .GreaterThan(0).WithMessage("El archivo no puede estar vacío")
                .LessThanOrEqualTo(10 * 1024 * 1024).WithMessage("El tamaño del archivo no puede exceder los 10MB");

            RuleFor(file => file.FileName)
                .Must(ValidateFileExtension)
                .WithMessage($"La extensión del archivo debe ser alguna de las siguientes: {string.Join(", ", allowedExtensions)}");

            RuleFor(file => file.ContentType)
                .Must(contentType => allowedContentTypes.Contains(contentType.ToLower()))
                .WithMessage($"El tipo de archivo debe ser alguno de los siguientes: {string.Join(", ", allowedContentTypes)}");

            // Validación avanzada usando los magic bytes
            /*RuleFor(file => file)
                .MustBeValidImage()
                .WithMessage("El archivo no es una imagen válida. Por favor, sube un archivo de imagen real.");*/
        }

        private bool ValidateFileExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }
    }
}
