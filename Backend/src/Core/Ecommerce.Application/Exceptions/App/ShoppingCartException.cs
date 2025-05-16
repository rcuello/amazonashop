namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Se lanza cuando hay un problema con el carrito de compras
    /// </summary>
    public class ShoppingCartException : ApplicationException
    {
        public ShoppingCartException(string message) : base(message)
        {

        }
    }
}
