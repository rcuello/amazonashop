using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Application.Identity;
using Ecommerce.Application.Models.Token;
using Ecommerce.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    public JwtSettings _jwtSettings {get;}
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<IAuthService> _logger;
    
    public AuthService(IHttpContextAccessor httpContextAccessor, IOptions<JwtSettings> jwtSettings, ILogger<IAuthService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string CreateToken(Usuario usuario, IList<string>? roles)
    {
        // Validar que la clave sea lo suficientemente larga para el algoritmo de firma
        var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.Key!);

        // Para HmacSha512 se necesitan al menos 64 bytes (512 bits)
        const int hmacSha512MinKeyLength = 64;
        // Para HmacSha256 se necesitan al menos 32 bytes (256 bits)
        const int hmacSha256MinKeyLength = 32;

        string algoritmo;
        if (keyBytes.Length >= hmacSha512MinKeyLength)
        {
            algoritmo = SecurityAlgorithms.HmacSha512Signature;
        }
        else if (keyBytes.Length >= hmacSha256MinKeyLength)
        {
            algoritmo = SecurityAlgorithms.HmacSha256Signature;            
            _logger.LogWarning(algoritmo, "Advertencia: La clave es demasiado corta para HmacSha512. Se usará HmacSha256 en su lugar.");
        }
        else
        {
            throw new ArgumentException(
                $"La clave JWT es demasiado corta. Para usar HmacSha256 se necesitan al menos {hmacSha256MinKeyLength} bytes, " +
                $"y para HmacSha512 se necesitan al menos {hmacSha512MinKeyLength} bytes. " +
                $"La clave actual tiene {keyBytes.Length} bytes.");
        }

        var claims = new List<Claim> {
            new Claim(JwtRegisteredClaimNames.NameId, usuario.UserName!),
            new Claim("userId", usuario.Id),
            new Claim("email", usuario.Email!)
        };

        foreach(var rol in roles!)
        {
            var claim = new Claim(ClaimTypes.Role, rol);
            claims.Add(claim);
        }

        var key = new SymmetricSecurityKey(keyBytes);
        var credenciales = new SigningCredentials(key, algoritmo);

        var tokenDescription = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(_jwtSettings.ExpireTime),
            SigningCredentials = credenciales
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescription);
        return tokenHandler.WriteToken(token);
    }

    public string GetSessionUser()
    {
        var username = _httpContextAccessor.HttpContext!.User?.Claims?
                .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        return username!;
    }
}