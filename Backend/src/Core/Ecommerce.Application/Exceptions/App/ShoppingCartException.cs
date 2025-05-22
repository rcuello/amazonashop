using System.Net;

namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Se lanza cuando hay un problema con el carrito de compras
    /// </summary>
    public class ShoppingCartException : ApplicationExceptionBase
    {
        public ShoppingCartException(string message)
            : base(message, HttpStatusCode.BadRequest) { }

        public ShoppingCartException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.BadRequest) { }
    }
}
