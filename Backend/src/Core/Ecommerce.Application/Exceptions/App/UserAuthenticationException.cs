namespace Ecommerce.Application.Exceptions.App
{
    /// <summary>
    /// Se lanza cuando hay un problema con la autenticación del usuario
    /// </summary>
    public class UserAuthenticationException : ApplicationException
    {
        public UserAuthenticationException(string message) : base(message) { }
        public UserAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
