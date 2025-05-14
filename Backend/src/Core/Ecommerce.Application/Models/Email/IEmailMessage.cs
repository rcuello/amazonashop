namespace Ecommerce.Application.Models.Email
{
    /// <summary>
    /// Interfaz base para todos los mensajes de correo electrónico
    /// </summary>
    public interface IEmailMessage
    {
        string To { get; set; }
        string Subject { get; set; }
        Task<string> RenderBodyAsync();
    }
}
