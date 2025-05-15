namespace Ecommerce.Application.Exceptions.App
{
    public class DatabaseConnectionException : ApplicationException
    {
        public DatabaseConnectionException(string message) : base(message) { }
        public DatabaseConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
