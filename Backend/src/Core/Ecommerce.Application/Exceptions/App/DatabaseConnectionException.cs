using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    public class DatabaseConnectionException : ApplicationExceptionBase
    {
        public DatabaseConnectionException(string message)
            : base(message, HttpStatusCode.ServiceUnavailable) { }

        public DatabaseConnectionException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.ServiceUnavailable) { }
    }
}
