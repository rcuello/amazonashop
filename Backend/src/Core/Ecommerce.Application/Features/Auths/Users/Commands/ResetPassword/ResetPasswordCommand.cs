using MediatR;

namespace Ecommerce.Application.Features.Auths.Users.Commands.ResetPassword;

public class ResetPasswordCommand : IRequest<Unit>
{
    public string? NewPassword { get; set; }

    public string? OldPassword { get; set; }
}