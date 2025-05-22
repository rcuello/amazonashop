using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Se lanza cuando hay un error en la creación de una orden
    /// </summary>
    public class OrderCreationException : ApplicationExceptionBase
    {

        public OrderCreationException(string message)
            : base(message, HttpStatusCode.BadRequest) { }

        public OrderCreationException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.BadRequest) { }
    }
}
