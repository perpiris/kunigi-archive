using KunigiArchive.Application.Abstractions.Exceptions;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Users.ResetPassword;

// Sets a new password from the emailed link. The password only changes if the link is valid and the new
// password passes the rules, so a failed attempt leaves the old one working.
public sealed class ResetPasswordHandler
{
    public const string InvalidLinkMessage = "Ο σύνδεσμος δεν ισχύει ή έχει λήξει. Ζητήστε νέο σύνδεσμο.";

    private readonly IUserAccounts _userAccounts;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(IUserAccounts userAccounts, ILogger<ResetPasswordHandler> logger)
    {
        _userAccounts = userAccounts;
        _logger = logger;
    }

    // Throws InvalidRequestException for an invalid or expired link, or a password that breaks the rules.
    public async Task HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Code))
        {
            throw new InvalidRequestException(InvalidLinkMessage);
        }

        Guid? userId = await _userAccounts.ResetPasswordAsync(
            command.Email.Trim(), command.Code, command.NewPassword, cancellationToken);

        if (userId is null)
        {
            throw new InvalidRequestException(InvalidLinkMessage);
        }

        _logger.LogInformation("Password reset by user {UserId}", userId);
    }
}

public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword);
