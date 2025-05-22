using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    public class DataAccessException : ApplicationExceptionBase
    {
        public DataAccessException(string message)
            : base(message, HttpStatusCode.InternalServerError) { }

        public DataAccessException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.InternalServerError) { }
    }
}
