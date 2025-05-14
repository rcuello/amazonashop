using Ecommerce.Application.Contracts.Infrastructure;

namespace Ecommerce.Application.Models.Email.Messages
{
    public class PasswordResetEmailMessage : IEmailMessage
    {
        private readonly ITemplateRender _templateRender;
        public string To { get; set; }
        public string Subject { get; set; } = "Cambiar el password";
        public string BodyContent { get; set; } = "Resetear el password, dale click aqui:";
        public string ResetLink { get; set; }

        public PasswordResetEmailMessage(ITemplateRender templateRender)
        {
            _templateRender = templateRender;
        }

        public async Task<string> RenderBodyAsync()
        {            
            return await _templateRender.RenderAsync("ResetPassword", this);
        }
    }
}
