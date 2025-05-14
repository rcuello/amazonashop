using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Models.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;


namespace Ecommerce.Infrastructure.MessageImplementation;

/// <summary>
/// Servicio de envío de correos electrónicos utilizando Mailtrap para entornos de desarrollo y pruebas
/// </summary>
public class MailtrapEmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<MailtrapEmailService> _logger;

    public MailtrapEmailService(IOptions<EmailSettings> emailSettings, ILogger<MailtrapEmailService> logger)
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
            _logger.LogError("Error al enviar correo: dirección de destino vacía o nula");
            return false;
        }

        // Validar configuración de Mailtrap
        if (string.IsNullOrWhiteSpace(_emailSettings.Host) || _emailSettings.Port <= 0)
        {
            _logger.LogError("Error al enviar correo: configuración de servidor SMTP (Mailtrap) incompleta");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Username) || string.IsNullOrWhiteSpace(_emailSettings.Password))
        {
            _logger.LogError("Error al enviar correo: credenciales de Mailtrap no configuradas");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Email) || string.IsNullOrWhiteSpace(_emailSettings.Email))
        {
            _logger.LogError("Error al enviar correo: configuración Email (Mailtrap) incompleta");
            return false;
        }

        try
        {
            _logger.LogInformation("Iniciando envío de correo a {EmailTo} con asunto '{Subject}' usando Mailtrap",
                email.To, email.Subject);

            // Renderizar el cuerpo del correo según el tipo específico de mensaje
            string renderedBody = await email.RenderBodyAsync();

            // Crear mensaje de correo
            using var mailMessage = new MailMessage()
            {
                From = new MailAddress(_emailSettings.Email, _emailSettings.DisplayName ?? "Ecommerce"),
                Subject = email.Subject,
                Body = renderedBody,
                IsBodyHtml = true
            };

            // Agregar destinatario
            mailMessage.To.Add(new MailAddress(email.To));

            // Configurar cliente SMTP para Mailtrap
            using var smtpClient = new SmtpClient()
            {
                Host = _emailSettings.Host,
                Port = _emailSettings.Port,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
            };

            // Establecer timeout para evitar bloqueos prolongados
            smtpClient.Timeout = 10000; // 10 segundos

            // Enviar correo de forma asíncrona
            await Task.Run(() => smtpClient.Send(mailMessage));

            _logger.LogInformation("Correo enviado exitosamente a {EmailTo} usando Mailtrap", email.To);
            return true;
        }
        catch (SmtpException ex)
        {
            // Manejar errores específicos de SMTP
            switch (ex.StatusCode)
            {
                case SmtpStatusCode.MailboxBusy:
                case SmtpStatusCode.MailboxUnavailable:
                    _logger.LogError("Error al enviar correo: buzón de destino ocupado o no disponible: {Message}", ex.Message);
                    break;
                case SmtpStatusCode.ExceededStorageAllocation:
                    _logger.LogError("Error al enviar correo: límite de almacenamiento excedido: {Message}", ex.Message);
                    break;
                default:
                    _logger.LogError(ex, "Error SMTP al enviar correo a {EmailTo}: {Message}", email.To, ex.Message);
                    break;
            }
            return false;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error de configuración al enviar correo: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción general al enviar correo a {EmailTo}: {Message}", email.To, ex.Message);
            return false;
        }
    }
}