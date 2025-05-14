using Ecommerce.Application.Contracts.Infrastructure;


namespace Ecommerce.Application.Models.Email.Messages
{
    public class WelcomeUserEmailMessage : IEmailMessage
    {
        private readonly ITemplateRender _templateRender;
        public string To { get; set; }
        public string Subject { get; set; } = "Bienvenido a Ecommerce";
        public string GettingStartedLink { get; set; }
        public string UserName { get; set; }

        public WelcomeUserEmailMessage(ITemplateRender templateRender)
        {
            _templateRender = templateRender;
        }

        public async Task<string> RenderBodyAsync()
        {
            return await _templateRender.RenderAsync("WelcomeUser", this);
        }
    }
}
