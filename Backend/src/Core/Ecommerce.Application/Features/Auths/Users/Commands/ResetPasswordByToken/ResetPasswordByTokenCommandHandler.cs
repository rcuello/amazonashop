using System.Text;
using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Exceptions.App;
using Ecommerce.Application.Models.Email;
using Ecommerce.Application.Models.Email.Messages;
using Ecommerce.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Application.Features.Auths.Users.Commands.ResetPasswordByToken;

public class ResetPasswordByTokenCommandHandler : IRequestHandler<ResetPasswordByTokenCommand, string>
{
    private readonly IEmailService _serviceEmail;
    private readonly ITemplateRender _templateRender;
    private readonly EmailSettings _emailSettings;
    private readonly UserManager<Usuario> _userManager;
    private readonly ILogger<ResetPasswordByTokenCommandHandler> _logger;


    public ResetPasswordByTokenCommandHandler(
        IOptions<EmailSettings> emailSettings,
        IEmailService serviceEmail,
        ITemplateRender templateRender,
        UserManager<Usuario> userManager,
        ILogger<ResetPasswordByTokenCommandHandler> logger)
    {
        _emailSettings = emailSettings?.Value ?? throw new ArgumentNullException(nameof(emailSettings));
        _serviceEmail = serviceEmail ?? throw new ArgumentNullException(nameof(serviceEmail));
        _templateRender = templateRender ?? throw new ArgumentNullException(nameof(templateRender));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(ResetPasswordByTokenCommand request, CancellationToken cancellationToken)
    {

        // Las validaciones iniciales por fluent

        var usuario = await FindUserByEmailAsync(request.Email!);

        // Decodificar token (ya validado por FluentValidation)
        var decodedToken = DecodeToken(request.Token!);

        // Resetear password
        await ResetPasswordAsync(usuario, decodedToken, request.Password!);

        // Enviar email de confirmación
        await SendConfirmationEmailAsync(usuario);

        _logger.LogInformation("Password reseteado exitosamente para el usuario {Email}", request.Email);


        return $"Se actualizo exitosamente tu password ${request.Email}";

    }

    private async Task<Usuario> FindUserByEmailAsync(string email)
    {
        try
        {
            var usuario = await _userManager.FindByEmailAsync(email);

            if (usuario is null)
            {
                _logger.LogWarning("Intento de reset de password para email no registrado: {Email}", email);
                throw new EntityNotFoundException($"No se encontró un usuario con el email: {email}");
            }

            return usuario;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Error al buscar usuario por email: {Email}", email);
            throw new BusinessLogicException("Error interno al buscar el usuario");
        }
    }

    private string DecodeToken(string token)
    {
        try
        {
            var tokenBytes = Convert.FromBase64String(token);
            return Encoding.UTF8.GetString(tokenBytes);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Token con formato inválido");
            throw new BadRequestException("El token proporcionado no tiene un formato válido");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al decodificar el token");
            throw new BusinessLogicException("Error interno al procesar el token");
        }
    }

    private async Task ResetPasswordAsync(Usuario usuario, string token, string newPassword)
    {
        try
        {
            var resetResult = await _userManager.ResetPasswordAsync(usuario, token, newPassword);

            if (!resetResult.Succeeded)
            {
                var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Falló el reset de password para {Email}. Errores: {Errors}",
                    usuario.Email, errors);

                if (resetResult.Errors.Any(e => e.Code.Contains("InvalidToken")))
                {
                    throw new BadRequestException("El token de restablecimiento es inválido o ha expirado");
                }

                throw new BadRequestException($"No se pudo restablecer el password: {errors}");
            }
        }
        catch (Exception ex) when (!(ex is BadRequestException))
        {
            _logger.LogError(ex, "Error inesperado al resetear password para {Email}", usuario.Email);
            throw new BusinessLogicException("Error interno al restablecer el password");
        }
    }

    private async Task SendConfirmationEmailAsync(Usuario usuario)
    {
        try
        {
            var emailMessage = new PasswordUpdatedEmailMessage(_templateRender)
            {
                To = usuario.Email,
                LoginLink = "https://tuapp.com/login",
                BodyContent = "Tu contraseña ha sido actualizada correctamente."
            };

            var emailResult = await _serviceEmail.SendEmail(emailMessage);

            if (!emailResult)
            {
                _logger.LogWarning("No se pudo enviar email de confirmación a {Email}", usuario.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de confirmación a {Email}", usuario.Email);
        }
    }
}