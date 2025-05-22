using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    public class BusinessLogicException : ApplicationExceptionBase
    {
        public BusinessLogicException(string message)
            : base(message, HttpStatusCode.InternalServerError) { }

        public BusinessLogicException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.InternalServerError) { }
    }
}
