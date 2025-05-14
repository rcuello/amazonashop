using System.Text;
using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Models.Email;
using Ecommerce.Application.Models.Email.Messages;
using Ecommerce.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Ecommerce.Application.Features.Auths.Users.Commands.ResetPasswordByToken;

public class ResetPasswordByTokenCommandHandler : IRequestHandler<ResetPasswordByTokenCommand, string>
{
    private readonly IEmailService _serviceEmail;
    private readonly ITemplateRender _templateRender;
    private readonly EmailSettings _emailSettings;
    private readonly UserManager<Usuario> _userManager;
    

    public ResetPasswordByTokenCommandHandler(IOptions<EmailSettings> emailSettings, IEmailService serviceEmail, ITemplateRender templateRender,UserManager<Usuario> userManager)
    {
        _emailSettings = emailSettings.Value ?? throw new ArgumentNullException(nameof(emailSettings));
        _serviceEmail = serviceEmail;
        _templateRender = templateRender;

        _userManager = userManager;
        
    }

    public async Task<string> Handle(ResetPasswordByTokenCommand request, CancellationToken cancellationToken)
    {

        if(!string.Equals(request.Password, request.ConfirmPassword))
        {
            throw new BadRequestException("El password no es igual a la confirmacion del password");
        }

        var updateUsuario = await _userManager.FindByEmailAsync(request.Email!);
        if(updateUsuario is null)
        {
            throw new BadRequestException("El email no esta registrado como usuario");
        }

        var token = Convert.FromBase64String(request.Token!);
        var tokenResult = Encoding.UTF8.GetString(token);

        var resetResultado = await  _userManager.ResetPasswordAsync(updateUsuario, tokenResult, request.Password!);

        if (resetResultado.Succeeded)
        {
            var emailMessage = new PasswordUpdatedEmailMessage(_templateRender)
            {
                To = updateUsuario.Email,
                LoginLink = "https://tuapp.com/login",
                BodyContent = "Tu contraseña ha sido actualizada correctamente."
            };

            var result = await _serviceEmail.SendEmail(emailMessage);
        }
        else
        {
            throw new Exception("No se pudo resetear el password");
        }
        

        return $"Se actualizo exitosamente tu password ${request.Email}";
 
    }
}