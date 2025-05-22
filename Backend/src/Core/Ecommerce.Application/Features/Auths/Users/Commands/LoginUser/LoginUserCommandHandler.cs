using AutoMapper;
using Ecommerce.Application.Exceptions.App;
using Ecommerce.Application.Features.Addresses.Vms;
using Ecommerce.Application.Features.Auths.Users.Vms;
using Ecommerce.Application.Identity;
using Ecommerce.Application.Persistence;
using Ecommerce.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Application.Features.Auths.Users.Commands.LoginUser;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResponse>
{
    private readonly UserManager<Usuario> _userManager;
    private readonly SignInManager<Usuario> _sigInManager;

    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly IAuthService _authService;

    private readonly IMapper _mapper;

    private readonly IUnitOfWork _unitOfWork;

    public LoginUserCommandHandler(
                        UserManager<Usuario> userManager,
                        SignInManager<Usuario> sigInManager,
                        RoleManager<IdentityRole> roleManager,
                        IAuthService authService,
                        IMapper mapper,
                        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _sigInManager = sigInManager;
        _roleManager = roleManager;
        _authService = authService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email!);
        if (user is null)
        {
            throw new UserBadCredentialsException("El email o contraseña son incorrectos");
        }

        if (!user.IsActive)
        {
            throw new UserUnauthorizedException("El usuario está bloqueado, contacte al administrador");
        }

        var resultado = await _sigInManager.CheckPasswordSignInAsync(user, request.Password!, false);

        if (!resultado.Succeeded)
        {
            if (resultado.IsLockedOut)
            {
                throw new UserUnauthorizedException("La cuenta está bloqueada temporalmente debido a múltiples intentos fallidos");
            }

            if (resultado.IsNotAllowed)
            {
                throw new UserUnauthorizedException("El inicio de sesión no está permitido. Verifique su cuenta");
            }

            if (resultado.RequiresTwoFactor)
            {
                throw new UserUnauthorizedException("Se requiere autenticación de dos factores");
            }

            throw new UserBadCredentialsException("El email o contraseña son incorrectos");
        }

        try
        {

            var direccionEnvio = await _unitOfWork.Repository<Address>().GetEntityAsync(
            x => x.Username == user.UserName
            );

            var roles = await _userManager.GetRolesAsync(user);

            var authResponse = new AuthResponse
            {
                Id = user.Id,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Telefono = user.Telefono,
                Email = user.Email,
                Username = user.UserName,
                Avatar = user.AvatarUrl,
                DireccionEnvio = _mapper.Map<AddressVm>(direccionEnvio),
                Token = _authService.CreateToken(user, roles),
                Roles = roles
            };

            return authResponse;
        }
        catch (Exception ex) when (!(ex is UserBadCredentialsException || ex is UnauthorizedAccessException))
        {
            throw new InvalidOperationException("Error interno durante el proceso de autenticación", ex);
        }

    }
}