using Ecommerce.Application.Features.Auths.Users.Vms;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Application.Features.Auths.Users.Commands.RegisterUser;

public class RegisterUserCommand : IRequest<AuthResponse>
{

    /// <summary>
    /// Nombre del usuario (requerido)
    /// </summary>
    public string? Nombre { get; set; }

    /// <summary>
    /// Apellido del usuario (requerido)
    /// </summary>
    public string? Apellido { get; set; }

    /// <summary>
    /// Dirección de correo electrónico (requerido)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Número de teléfono (opcional)
    /// </summary>
    public string? Telefono { get; set; }

    /// <summary>
    /// Archivo de imagen para foto de perfil (opcional)
    /// </summary>
    public IFormFile? Foto {get;set;}
    
    public string? FotoUrl {get;set;}

    public string? FotoId {get;set;}

    /// <summary>
    /// Contraseña del usuario (requerido)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Nombre de usuario único (requerido)
    /// </summary>
    public string? Username {get;set;}

}