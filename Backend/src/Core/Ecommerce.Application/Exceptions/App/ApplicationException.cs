using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    public class ApplicationException : ApplicationExceptionBase
    {
        public ApplicationException(string message)
            : base(message, HttpStatusCode.InternalServerError) { }

        public ApplicationException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.InternalServerError) { }
    }
}
