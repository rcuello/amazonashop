using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Se lanza cuando hay un error en la integración con Stripe
    /// </summary>
    public class PaymentProcessingException : ApplicationExceptionBase
    {        

        public PaymentProcessingException(string message, string? paymentIntentId = null)
            : base(message, HttpStatusCode.BadRequest) 
        {
            PaymentIntentId = paymentIntentId;
        }

        public PaymentProcessingException(string message, Exception innerException, string? paymentIntentId = null)
            : base(message, innerException, HttpStatusCode.BadRequest) 
        {
            PaymentIntentId = paymentIntentId;
        }

        public string? PaymentIntentId { get; }
    }
}
