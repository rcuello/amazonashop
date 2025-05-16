namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Se lanza cuando hay un error en la creación de una orden
    /// </summary>
    public class OrderCreationException : ApplicationException
    {
        public OrderCreationException(string message) : base(message)
        {
        }
    }
}
