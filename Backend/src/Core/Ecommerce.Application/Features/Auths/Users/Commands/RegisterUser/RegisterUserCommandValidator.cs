using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Ecommerce.Application.Features.Auths.Users.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"^(\+?1[-.\s]?)?(\(?\d{3}\)?[-.\s]?)?\d{3}[-.\s]?\d{4}$",RegexOptions.Compiled);
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public RegisterUserCommandValidator()
    {
        // Validación de Nombre
        RuleFor(x => x.Nombre)
            .NotEmpty()
            .WithMessage("El nombre es requerido")
            .Length(2, 100)
            .WithMessage("El nombre debe tener entre 2 y 100 caracteres");
            //.Matches(@"^[a-zA-ZáéíóúüñÁÉÍÓÚÜÑ\s]+$")
            //.WithMessage("El nombre solo puede contener letras y espacios");

        // Validación de Apellido
        RuleFor(x => x.Apellido)
            .NotEmpty()
            .WithMessage("El apellido es requerido")
            .Length(2, 100)
            .WithMessage("El apellido debe tener entre 2 y 100 caracteres");
            //.Matches(@"^[a-zA-ZáéíóúüñÁÉÍÓÚÜÑ\s]+$")
            //.WithMessage("El apellido solo puede contener letras y espacios");

        // Validación de Email
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .Must(BeValidEmail)
            .WithMessage("El formato del email no es válido")
            .MaximumLength(255)
            .WithMessage("El email no puede exceder 255 caracteres");

        // Validación de Username
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("El nombre de usuario es requerido")
            .Length(3, 50)
            .WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres");
            //.Matches(@"^[a-zA-Z0-9._-]+$")
            //.WithMessage("El nombre de usuario solo puede contener letras, números, puntos, guiones y guiones bajos");

        // Validación de Password
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres")
            .MaximumLength(128)
            .WithMessage("La contraseña no puede exceder 128 caracteres")
            .Must(HaveValidPasswordComplexity)
            .WithMessage("La contraseña debe contener al menos: una mayúscula, una minúscula, un número y un carácter especial");

        // Validación de Teléfono (opcional)
        RuleFor(x => x.Telefono)
            .Must(BeValidPhoneWhenProvided)
            .WithMessage("El formato del teléfono no es válido")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

        // Validación de Foto (opcional)
        RuleFor(x => x.Foto)
            .Must(BeValidImageFile)
            .WithMessage($"La imagen debe ser de tipo: {string.Join(", ", AllowedImageExtensions)} y no exceder {MaxFileSize / (1024 * 1024)}MB")
            .When(x => x.Foto is not null);
    }

    private static bool BeValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailRegex.IsMatch(email);
    }

    private static bool BeValidPhoneWhenProvided(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return true; // Es opcional

        return PhoneRegex.IsMatch(phone);
    }

    private static bool HaveValidPasswordComplexity(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        var hasLower = password.Any(char.IsLower);
        var hasUpper = password.Any(char.IsUpper);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasLower && hasUpper && hasDigit && hasSpecial;
    }

    private static bool BeValidImageFile(IFormFile? file)
    {
        if (file is null)
            return true; // Es opcional

        // Validar tamaño
        if (file.Length > MaxFileSize)
            return false;

        // Validar extensión
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            return false;

        return AllowedImageExtensions.Contains(extension);
    }
}