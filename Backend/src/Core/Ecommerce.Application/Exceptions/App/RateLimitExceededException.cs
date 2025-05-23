using System.Net;


namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Excepción personalizada para rate limiting
    /// </summary>
    public class RateLimitExceededException : ApplicationExceptionBase
    {
        public string RequestTypeName { get; }
        public int MaxRequests { get; }
        public int WindowMinutes { get;}

        public RateLimitExceededException(string message,string requestTypeName,int maxRequests,int windowMinutes)
           : base(message, HttpStatusCode.TooManyRequests) 
        { 
            this.RequestTypeName = requestTypeName;
            this.MaxRequests = maxRequests;
            this.WindowMinutes = windowMinutes;
        }

        public RateLimitExceededException(string message, string requestTypeName, int maxRequests, int windowMinutes, Exception innerException)
            : base(message, innerException, HttpStatusCode.BadRequest) 
        {
            this.RequestTypeName = requestTypeName;
            this.MaxRequests = maxRequests;
            this.WindowMinutes = windowMinutes;
        }
    }
}
