using Ecommerce.Application.Features.Auths.Users.Commands.UpdateAdminStatusUser;
using Ecommerce.Application.Features.Auths.Users.Commands.UpdateAdminUser;
using Ecommerce.Application.Features.Auths.Users.Queries.GetUserByUsername;
using Ecommerce.Application.Features.Auths.Users.Queries.PaginationUsers;
using Ecommerce.Application.Features.Auths.Users.Vms;
using Ecommerce.Application.Features.Shared.Queries;
using Ecommerce.Application.Models.Authorization;
using Ecommerce.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = Role.ADMIN)]
[Route("api/v1/[controller]")]
public class AdminUserController : ControllerBase
{
    private readonly IMediator _mediator;
    

    public AdminUserController(IMediator mediator)
    {
        _mediator = mediator;        
    }
    
    [HttpGet("username/{username}")]
    [ProducesResponseType(typeof(AuthResponse), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<AuthResponse>> GetUsuarioByUsername(string username)
    {
        var query = new GetUserByUsernameQuery(username);
        return await _mediator.Send(query);
    }

    
    [HttpGet("paginationAdmin")]
    [ProducesResponseType(typeof(PaginationVm<Usuario>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PaginationVm<Usuario>>> PaginationUser(
                    [FromQuery] PaginationUsersQuery paginationUsersQuery
                )
    {
        var paginationUser = await _mediator.Send(paginationUsersQuery);
        return Ok(paginationUser);
    }
    
    [HttpPut("updateAdminUser")]
    [ProducesResponseType(typeof(Usuario), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<Usuario>> UpdateAdminUser([FromBody] UpdateAdminUserCommand request)
    {
        return await _mediator.Send(request);
    }
    
    [HttpPut("updateAdminStastusUser")]
    [ProducesResponseType(typeof(Usuario), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<Usuario>> UpdateAdminStastusUser([FromBody] UpdateAdminStatusUserCommand request)
    {
        return await _mediator.Send(request);
    }
}
