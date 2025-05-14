using Ecommerce.Application.Contracts.Infrastructure;


namespace Ecommerce.Application.Models.Email.Messages
{
    public class PasswordUpdatedEmailMessage : IEmailMessage
    {
        private readonly ITemplateRender _templateRender;
        public string To { get; set; }
        public string Subject { get; set; } = "Tu contraseña ha sido actualizada correctamente";
        public string BodyContent { get; set; } = "Tu contraseña ha sido actualizada correctamente.";
        public string LoginLink { get; set; }

        public PasswordUpdatedEmailMessage(ITemplateRender templateRender)
        {
            _templateRender = templateRender;
        }

        public async Task<string> RenderBodyAsync()
        {
            return await _templateRender.RenderAsync("PasswordUpdated", this);
        }
    }
}
