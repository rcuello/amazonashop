using Ecommerce.Application.Models.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Application.Extensions;

public static class FluentEmailExtensions
{
    public static IServiceCollection AddServiceEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailFluentSettings>(configuration.GetSection(nameof(EmailFluentSettings)));

        var emailSettings = configuration.GetSection(nameof(EmailFluentSettings));
        var fromEmail = emailSettings.GetValue<string>("Email");
        var host = emailSettings.GetValue<string>("Host");
        var port = emailSettings.GetValue<int>("Port");

        services.AddFluentEmail(fromEmail)
                .AddSmtpSender(host, port);

        return services;
    }
}