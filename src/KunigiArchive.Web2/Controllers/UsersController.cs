using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Users;
using KunigiArchive.Application.Users.ChangeUserRole;
using KunigiArchive.Application.Users.CreateUser;
using KunigiArchive.Application.Users.GetUserDetails;
using KunigiArchive.Application.Users.GetUsers;
using KunigiArchive.Application.Users.SendTemporaryPassword;
using KunigiArchive.Web2.Contracts.Users;
using KunigiArchive.Web2.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web2.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UsersPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] GetUsersRequest request,
        [FromServices] GetUsersHandler handler,
        CancellationToken cancellationToken)
    {
        UsersPage page = await handler.HandleAsync(request.ToQuery(), cancellationToken);
        return Ok(page.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<CreateUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateUser(
        CreateUserRequest request,
        [FromServices] CreateUserHandler handler,
        CancellationToken cancellationToken)
    {
        CreateUserResult result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id = result.UserId }, result.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetUser(
        Guid id,
        [FromServices] GetUserDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        UserDetails user = await handler.HandleAsync(id, cancellationToken);
        return Ok(user.ToResponse());
    }

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ChangeUserRole(
        Guid id,
        ChangeUserRoleRequest request,
        [FromServices] ChangeUserRoleHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new ChangeUserRoleCommand(id, request.Role), cancellationToken);
        return NoContent();
    }

    // 502 when the email fails: the password has already changed, so calling again sends a new one.
    [HttpPost("{id:guid}/temporary-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway, "application/problem+json")]
    public async Task<IActionResult> SendTemporaryPassword(
        Guid id,
        [FromServices] SendTemporaryPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
