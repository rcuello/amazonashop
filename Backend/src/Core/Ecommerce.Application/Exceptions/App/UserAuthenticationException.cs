using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Se lanza cuando hay un problema con la autenticación del usuario
    /// </summary>
    public class UserAuthenticationException : ApplicationExceptionBase
    {
        public UserAuthenticationException(string message)
            : base(message, HttpStatusCode.Unauthorized) { }

        public UserAuthenticationException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.Unauthorized) { }
    }
}
