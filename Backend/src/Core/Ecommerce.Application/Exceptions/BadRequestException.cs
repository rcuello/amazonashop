using Ecommerce.Application.Exceptions.App;
using System.Net;

namespace Ecommerce.Application.Exceptions;
public class BadRequestException : ApplicationExceptionBase
{

    public BadRequestException(string message)
            : base(message, HttpStatusCode.BadRequest) { }

    public BadRequestException(string message, Exception innerException)
        : base(message, innerException, HttpStatusCode.BadRequest) { }

}