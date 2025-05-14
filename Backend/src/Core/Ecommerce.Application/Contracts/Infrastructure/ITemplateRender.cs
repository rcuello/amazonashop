using System.Threading;

namespace Ecommerce.Application.Contracts.Infrastructure
{
    public interface ITemplateRender
    {
        Task<string> RenderAsync<T>(string templateName, T model,CancellationToken cancellationToken = default);
    }
}
