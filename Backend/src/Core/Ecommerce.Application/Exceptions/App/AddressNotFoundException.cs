namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Se lanza cuando no se encuentra la dirección del usuario
    /// </summary>
    public class AddressNotFoundException : ApplicationException
    {
        public string Username { get; }
        public AddressNotFoundException(string username) : base($"No se encontró dirección para el usuario '{username}'") 
        {
            Username = username;
        }        
    }
}
