using Ecommerce.Application.Exceptions.App;
using System.Net;

namespace Ecommerce.Application.Exceptions;

public class EntityNotFoundException: ApplicationExceptionBase
{
    public EntityNotFoundException(string name, object key)
            : base($"La entidad '{name}' con clave ({key}) no fue encontrada", HttpStatusCode.NotFound) { }

    public EntityNotFoundException(string message)
        : base(message, HttpStatusCode.NotFound) { }
}