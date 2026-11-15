using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Users.ResetPassword;

// "Forgot password": emails a reset link if the address has an account. Open to anyone, so it never says
// whether the account exists or whether the email went out; the page shows the same message either way.
public sealed class RequestPasswordResetHandler
{
    private readonly IUserAccounts _userAccounts;
    private readonly IAccountEmails _accountEmails;
    private readonly ILogger<RequestPasswordResetHandler> _logger;

    public RequestPasswordResetHandler(
        IUserAccounts userAccounts,
        IAccountEmails accountEmails,
        ILogger<RequestPasswordResetHandler> logger)
    {
        _userAccounts = userAccounts;
        _accountEmails = accountEmails;
        _logger = logger;
    }

    public async Task HandleAsync(string email, CancellationToken cancellationToken)
    {
        string normalized = UserRules.NormalizeEmail(email);

        PasswordResetCode? reset = await _userAccounts.CreatePasswordResetCodeAsync(normalized, cancellationToken);
        if (reset is null)
        {
            return;
        }

        try
        {
            await _accountEmails.SendPasswordResetAsync(reset.Email, reset.Code, cancellationToken);
            _logger.LogInformation("Password reset link sent to user {UserId}", reset.UserId);
        }
        catch (EmailDeliveryException exception)
        {
            _logger.LogError(exception, "Password reset email to user {UserId} could not be sent", reset.UserId);
        }
    }
}
