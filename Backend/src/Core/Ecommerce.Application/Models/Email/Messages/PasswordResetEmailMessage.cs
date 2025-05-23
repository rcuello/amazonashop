using Ecommerce.Application.Contracts.Infrastructure;

namespace Ecommerce.Application.Models.Email.Messages
{
    public class PasswordResetEmailMessage : IEmailMessage
    {
        private readonly ITemplateRender _templateRender;
        public required string To { get; set; }
        public required string Subject { get; set; }
        public required string BodyContent { get; set; } = "Resetear el password, dale click aqui:";
        public required string ResetLink { get; set; }

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
