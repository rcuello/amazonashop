using FluentValidation;

namespace Ecommerce.Application.Features.Auths.Users.Commands.ResetPasswordByToken
{
    public class ResetPasswordByTokenCommandValidator : AbstractValidator<ResetPasswordByTokenCommand>
    {
        public ResetPasswordByTokenCommandValidator()
        {
            RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .MaximumLength(256)
            .WithMessage("El email no puede exceder los 256 caracteres");

            RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("El token es requerido")
            .Must(BeValidBase64)
            .WithMessage("El token no tiene un formato válido");

            RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("El password es requerido")
            .MinimumLength(8)
            .WithMessage("El password debe tener al menos 8 caracteres")
            .MaximumLength(128)
            .WithMessage("El password no puede exceder los 128 caracteres")
            .Matches(@"[A-Z]")
            .WithMessage("El password debe contener al menos una letra mayúscula")
            .Matches(@"[a-z]")
            .WithMessage("El password debe contener al menos una letra minúscula")
            .Matches(@"[0-9]")
            .WithMessage("El password debe contener al menos un número")
            .Matches(@"[^a-zA-Z0-9]")
            .WithMessage("El password debe contener al menos un carácter especial");

            RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("La confirmación del password es requerida")
            .Equal(x => x.Password)
            .WithMessage("El password no coincide con la confirmación");

            // Validación a nivel de objeto
            /*RuleFor(x => x)
                .Must(HaveMatchingPasswords)
                .WithMessage("El password y la confirmación deben coincidir")
                .OverridePropertyName("ConfirmPassword");*/
        }

        private static bool BeValidBase64(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                Convert.FromBase64String(token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool HaveMatchingPasswords(ResetPasswordByTokenCommand command)
        {
            return !string.IsNullOrEmpty(command.Password) &&
                   !string.IsNullOrEmpty(command.ConfirmPassword) &&
                   command.Password == command.ConfirmPassword;
        }
    }
}
