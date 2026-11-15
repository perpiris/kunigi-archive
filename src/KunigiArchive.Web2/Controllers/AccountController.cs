using KunigiArchive.Application.Users;
using KunigiArchive.Application.Users.MyAccount;
using KunigiArchive.Web2.Contracts.Account;
using KunigiArchive.Web2.Contracts.Users;
using KunigiArchive.Web2.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web2.Controllers;

// The signed-in user's own account. Both changes end the user's sessions, this one included.
[ApiController]
[Route("api/account")]
[Authorize]
[Produces("application/json")]
public sealed class AccountController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAccount(
        [FromServices] GetMyAccountHandler handler,
        CancellationToken cancellationToken)
    {
        UserAccount account = await handler.HandleAsync(cancellationToken);
        return Ok(account.ToResponse());
    }

    [HttpPut("email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ChangeEmail(
        ChangeEmailRequest request,
        [FromServices] ChangeMyEmailHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new ChangeMyEmailCommand(request.CurrentPassword, request.NewEmail), cancellationToken);
        return NoContent();
    }

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        [FromServices] ChangeMyPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new ChangeMyPasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return NoContent();
    }
}