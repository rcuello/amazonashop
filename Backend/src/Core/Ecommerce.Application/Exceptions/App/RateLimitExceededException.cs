using System.Net;


namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Excepción personalizada para rate limiting
    /// </summary>
    public class RateLimitExceededException : ApplicationExceptionBase
    {
        public RateLimitExceededException(string message)
           : base(message, HttpStatusCode.TooManyRequests) { }

        public RateLimitExceededException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.BadRequest) { }
    }
}
