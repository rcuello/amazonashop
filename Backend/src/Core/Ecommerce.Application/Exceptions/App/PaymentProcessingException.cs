namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Se lanza cuando hay un error en la integración con Stripe
    /// </summary>
    public class PaymentProcessingException : ApplicationException
    {
        public PaymentProcessingException(string message, string? paymentIntentId = null)
            : base(message)
        {
            PaymentIntentId = paymentIntentId;
        }

        public string? PaymentIntentId { get; }
    }
}
