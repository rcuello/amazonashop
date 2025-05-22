using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    public class UserUnauthorizedException : ApplicationExceptionBase
    {
        public UserUnauthorizedException(string message)
            : base(message, HttpStatusCode.Forbidden) { }

        public UserUnauthorizedException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.Forbidden) { }
    }
}
