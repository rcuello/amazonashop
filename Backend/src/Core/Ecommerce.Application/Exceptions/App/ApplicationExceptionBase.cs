using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    public abstract class ApplicationExceptionBase : Exception
    {
        public HttpStatusCode StatusCode { get; }
        protected ApplicationExceptionBase(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            : base(message)
        {
            StatusCode = statusCode;
        }

        protected ApplicationExceptionBase(string message, Exception innerException, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
