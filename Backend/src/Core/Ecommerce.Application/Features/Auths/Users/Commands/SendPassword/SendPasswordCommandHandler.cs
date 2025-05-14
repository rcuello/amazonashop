using System.Text;
using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Models.Email;
using Ecommerce.Application.Models.Email.Messages;
using Ecommerce.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using Stripe;

namespace Ecommerce.Application.Features.Auths.Users.Commands.SendPassword;

public class SendPasswordCommandHandler : IRequestHandler<SendPasswordCommand, string>
{
    private readonly IEmailService _serviceEmail;
    private readonly ITemplateRender _templateRender;
    private readonly UserManager<Usuario> _userManager;
    private readonly EmailSettings _emailSettings;

    public SendPasswordCommandHandler(IOptions<EmailSettings> emailSettings,IEmailService serviceEmail, UserManager<Usuario> userManager, ITemplateRender templateRender)
    {
        _emailSettings = emailSettings.Value ?? throw new ArgumentNullException(nameof(emailSettings));
        _serviceEmail = serviceEmail;
        _userManager = userManager;
        _templateRender = templateRender;
    }

    public async Task<string> Handle(SendPasswordCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByEmailAsync(request.Email!);
        if(usuario is null)
        {
            throw new BadRequestException("El usuario no existe");
        }

        var token = await GenerateTokenAsync(usuario);        

        var resetUrl = $"{_emailSettings.BaseUrlClient}/password/reset/{token}";

        var emailMessage = new PasswordResetEmailMessage(_templateRender)
        {
            To = request.Email,
            ResetLink = resetUrl,
            BodyContent = "Resetear el password, dale click aqui:"
        };

        var result = await _serviceEmail.SendEmail(emailMessage);

        if(!result)
        {
            throw new Exception("No se pudo enviar el email");
        }

        return $"Se envio el email a la cuenta {request.Email}";
    }

    private async Task<string> GenerateTokenAsync(Usuario usuario)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var plainTextBytes = Encoding.UTF8.GetBytes(token);
        token = Convert.ToBase64String(plainTextBytes);

        return token;
    }
}