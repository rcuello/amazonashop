using Ecommerce.Application.Validators.Files;
using FluentValidation;

namespace Ecommerce.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private readonly string[] _allowedContentTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif" };
    private const int MaxFileSizeMb = 10;
    private const int MaxFileSize = MaxFileSizeMb * 1024 * 1024;

    public CreateProductCommandValidator()
    {
        RuleFor(p => p.Nombre)
            .NotEmpty().WithMessage("El nombre no puede estar en blanco")
            .MaximumLength(50).WithMessage("El nombre no puede exceder los 50 caracteres");

        RuleFor(p => p.Descripcion)
            .NotEmpty().WithMessage("La descripcion no puede ser nula");


        RuleFor(p => p.Stock)
            .NotEmpty().WithMessage("El stock no puede ser nulo");

        RuleFor(p => p.Precio)
            .NotEmpty().WithMessage("El precio no puede ser nulo");

        When(p => p.Imagenes != null && p.Imagenes.Any(), () => { 
            RuleFor(p=> p.Imagenes)
                .NotNull()
                .Must(files => files != null && files.All(f => f.Length > 0 && f.Length <= MaxFileSize))
                .WithMessage($"Las imágenes no pueden estar vacías y deben ser menores a {MaxFileSizeMb}MB");

            RuleForEach(p => p.Imagenes)
                .SetValidator(new ImageFileValidator(_allowedExtensions, _allowedContentTypes));
        });

    }
}