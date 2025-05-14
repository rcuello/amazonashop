using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Features.Auths.Users.Vms;
using Ecommerce.Application.Identity;
using Ecommerce.Application.Models.Email;
using Ecommerce.Application.Models.Email.Messages;
using Ecommerce.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Ecommerce.Application.Features.Auths.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    private readonly IEmailService _serviceEmail;
    private readonly ITemplateRender _templateRender;
    private readonly EmailSettings _emailSettings;
    private readonly UserManager<Usuario> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly IAuthService _authService;

    public RegisterUserCommandHandler(
                        IOptions<EmailSettings> emailSettings, 
                        IEmailService serviceEmail, 
                        ITemplateRender templateRender,
                        UserManager<Usuario> userManager, 
                        RoleManager<IdentityRole> roleManager, 
                        IAuthService authService
                        )
    {
        _emailSettings = emailSettings.Value ?? throw new ArgumentNullException(nameof(emailSettings));
        _serviceEmail = serviceEmail;
        _templateRender = templateRender;

        _userManager = userManager;
        _roleManager = roleManager;
        _authService = authService;
    }

    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existeUserByEmail = await _userManager.FindByEmailAsync(request.Email!) is null ? false : true;
        if(existeUserByEmail)
        {
            throw new BadRequestException("El Email del usuario ya existe en la base de datos");
        }

        var existeUserByUsername = await _userManager.FindByNameAsync(request.Username!) is null ? false : true;
        if(existeUserByEmail)
        {
            throw new BadRequestException("El Username del usuario ya existe en la base de datos");
        }
        
        var usuario = new Usuario 
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Telefono = request.Telefono,
            Email = request.Email,
            UserName = request.Username,
            AvatarUrl = request.FotoUrl
        };

        var resultado = await _userManager.CreateAsync(usuario!, request.Password!);

        if(resultado.Succeeded)
        {
            await _userManager.AddToRoleAsync(usuario, AppRole.GenericUser);
            var roles = await _userManager.GetRolesAsync(usuario);

            var emailMessage = new WelcomeUserEmailMessage(_templateRender)
            {
                To = request.Email,
                UserName=usuario.UserName,
                GettingStartedLink = "https://tuapp.com/empezar",                
            };

            var result = await _serviceEmail.SendEmail(emailMessage);

            return new AuthResponse
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.Telefono,
                Email = usuario.Email,
                Username = usuario.UserName,
                Avatar = usuario.AvatarUrl,
                Token = _authService.CreateToken(usuario, roles),
                Roles = roles
            };

        }

        throw new Exception("No se pudo registrar el usuario");

    }
}