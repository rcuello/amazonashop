using Ecommerce.Application.Models.Email;

namespace Ecommerce.Application.Contracts.Infrastructure;

public interface IEmailService
{
    Task<bool> SendEmail(IEmailMessage emailMessage);
}