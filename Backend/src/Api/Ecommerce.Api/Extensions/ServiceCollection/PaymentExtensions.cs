using Ecommerce.Application.Models.Payment;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class PaymentExtensions
{
    public static IServiceCollection AddCustomPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Servicios de pagos
        services.Configure<StripeSettings>(configuration.GetSection("StripeSettings"));

        // Agregar servicios de Stripe
        //services.AddScoped<IManagePaymentService, StripePaymentService>();
        return services;
    }
}
