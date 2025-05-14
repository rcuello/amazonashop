using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Models.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using SendGrid;
using System.Net;

namespace Ecommerce.Infrastructure;

public class SendgridEmailService : IEmailService
{
    public EmailSettings _emailSettings { get; }

    public ILogger<SendgridEmailService> _logger { get; }

    public SendgridEmailService(IOptions<EmailSettings> emailSettings, ILogger<SendgridEmailService> logger)
    {
        _emailSettings = emailSettings.Value ?? throw new ArgumentNullException(nameof(emailSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendEmail(IEmailMessage email)
    {
        if (email == null)
        {
            _logger.LogError("Error al enviar correo: el objeto email es nulo");
            return false;
        }

        if (string.IsNullOrWhiteSpace(email.To))
        {
            _logger.LogError("Error al enviar correo: direcci�n de destino vac�a o nula");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Key))
        {
            _logger.LogError("Error al enviar correo: API Key de SendGrid no configurada");
            return false;
        }

        try
        {
            _logger.LogInformation("Iniciando env�o de correo a {EmailTo} con asunto '{Subject}'", email.To, email.Subject);

            var client = new SendGridClient(_emailSettings.Key);
            var from = new EmailAddress(_emailSettings.Email);
            var subject = email.Subject;
            var to = new EmailAddress(email.To, email.To);

            // Renderizar el cuerpo del correo seg�n el tipo espec�fico de mensaje
            string htmlContent = await email.RenderBodyAsync();

            var plainTextContent = string.Empty;            

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Correo enviado exitosamente a {EmailTo}", email.To);
                return true;
            }
            else
            {
                var statusCode = response.StatusCode;
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Error al enviar correo: c�digo de estado {StatusCode}, respuesta: {Response}",
                    statusCode, responseBody);

                // Manejar c�digos de estado espec�ficos
                switch (statusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        _logger.LogError("API Key de SendGrid inv�lida o no autorizada");
                        break;
                    case HttpStatusCode.BadRequest:
                        _logger.LogError("Solicitud mal formada. Verifique los datos del correo");
                        break;

                    case HttpStatusCode.Forbidden:
                        _logger.LogError("Error de permisos en SendGrid: Es posible que la direcci�n del remitente no est� verificada");

                        if (responseBody.Contains("does not match a verified Sender Identity"))
                        {
                            _logger.LogError("La direcci�n de correo remitente '{SenderEmail}' no est� verificada en SendGrid. " +
                                            "Debe verificar este dominio o direcci�n de correo en la configuraci�n de Sender Identity de SendGrid. " +
                                            "Visite https://sendgrid.com/docs/for-developers/sending-email/sender-identity/ para m�s informaci�n",
                                            _emailSettings.Email);
                        }
                        break;
                    case HttpStatusCode.TooManyRequests:
                        _logger.LogError("L�mite de tasa de env�o de SendGrid excedido");
                        break;
                }

                return false;
            }                
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepci�n al enviar correo a {EmailTo}: {ErrorMessage}",
                email.To, ex.Message);

            

            return false;
        }


    }
}
