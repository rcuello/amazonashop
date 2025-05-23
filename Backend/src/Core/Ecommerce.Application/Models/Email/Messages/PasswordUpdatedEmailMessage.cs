using Ecommerce.Application.Contracts.Infrastructure;


namespace Ecommerce.Application.Models.Email.Messages
{
    public class PasswordUpdatedEmailMessage : IEmailMessage
    {
        private readonly ITemplateRender _templateRender;
        public required string To { get; set; }
        public required string Subject { get; set; }
        public required string BodyContent { get; set; }
        public required string LoginLink { get; set; }

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
