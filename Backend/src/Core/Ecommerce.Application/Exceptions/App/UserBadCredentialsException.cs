using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    public class UserBadCredentialsException : ApplicationExceptionBase
    {
        public UserBadCredentialsException()
            : base("Las credenciales proporcionadas son incorrectas", HttpStatusCode.Unauthorized) { }

        public UserBadCredentialsException(string message)
            : base(message, HttpStatusCode.Unauthorized) { }

        public UserBadCredentialsException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.Unauthorized) { }
    }
}
