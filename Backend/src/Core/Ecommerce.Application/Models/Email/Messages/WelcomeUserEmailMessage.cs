using Ecommerce.Application.Contracts.Infrastructure;


namespace Ecommerce.Application.Models.Email.Messages
{
    public class WelcomeUserEmailMessage : IEmailMessage
    {
        private readonly ITemplateRender _templateRender;
        public required string To { get; set; }
        public required string Subject { get; set; }
        public required string GettingStartedLink { get; set; }
        public required string UserName { get; set; }

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
