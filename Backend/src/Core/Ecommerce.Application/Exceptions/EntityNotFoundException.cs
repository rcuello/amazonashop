namespace Ecommerce.Application.Exceptions;

public class EntityNotFoundException: ApplicationException
{
    public EntityNotFoundException(string name, object key): base($"Entity \"{name}\" ({key}) no fue encontrado")
    {
    }
}