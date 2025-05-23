using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Identity;
using Ecommerce.Application.Models.Email;
using Ecommerce.Application.Models.Email.Messages;
using Ecommerce.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Ecommerce.Application.Features.Auths.Users.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IEmailService _serviceEmail;
    private readonly ITemplateRender _templateRender;
    private readonly EmailSettings _emailSettings;
    private readonly UserManager<Usuario> _userManager;
    private readonly IAuthService _authService;

    public ResetPasswordCommandHandler(IOptions<EmailSettings> emailSettings, IEmailService serviceEmail, ITemplateRender templateRender,UserManager<Usuario> userManager, IAuthService authService)
    {
        _emailSettings = emailSettings.Value ?? throw new ArgumentNullException(nameof(emailSettings));
        _serviceEmail = serviceEmail;
        _templateRender = templateRender;

        _userManager = userManager;
        _authService = authService;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var updateUsuario = await _userManager.FindByNameAsync(_authService.GetSessionUser());
        if(updateUsuario is null)
        {
            throw new BadRequestException("El usuario no existe");
        }

       var resultValidateOldPassword = _userManager.PasswordHasher
            .VerifyHashedPassword(updateUsuario, updateUsuario.PasswordHash!, request.OldPassword!);

        if(  !(resultValidateOldPassword == PasswordVerificationResult.Success)  )
        {
            throw new BadRequestException("El actual password ingresado es erroneo");
        }

        var hashedNewPassword = _userManager.PasswordHasher.HashPassword(updateUsuario, request.NewPassword!);
        updateUsuario.PasswordHash = hashedNewPassword;

        var resultado = await _userManager.UpdateAsync(updateUsuario);

        if (resultado.Succeeded)
        {
            var emailMessage = new PasswordUpdatedEmailMessage(_templateRender)
            {
                To          = updateUsuario.Email!,
                Subject     = "Tu contraseña ha sido actualizada correctamente.",
                LoginLink   = "https://tuapp.com/login",
                BodyContent = "Tu contraseña ha sido actualizada correctamente."
            };

            var result = await _serviceEmail.SendEmail(emailMessage);
        }
        else
        {
            throw new Exception("No se pudo resetear el password");
        }        

        return Unit.Value;              
    }
}