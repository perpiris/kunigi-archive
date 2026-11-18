using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Authorization;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Users.SendTemporaryPassword;

// For a user who never got (or lost) the account email: a new temporary password replaces the current one.
public sealed class SendTemporaryPasswordHandler
{
    private readonly IUserQueries _userQueries;
    private readonly IUserAccounts _userAccounts;
    private readonly IAccountEmails _accountEmails;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SendTemporaryPasswordHandler> _logger;

    public SendTemporaryPasswordHandler(
        IUserQueries userQueries,
        IUserAccounts userAccounts,
        IAccountEmails accountEmails,
        ICurrentUser currentUser,
        ILogger<SendTemporaryPasswordHandler> logger)
    {
        _userQueries = userQueries;
        _userAccounts = userAccounts;
        _accountEmails = accountEmails;
        _currentUser = currentUser;
        _logger = logger;
    }

    // Throws EmailDeliveryException when the email fails. The password has changed by then, so the admin
    // should simply try again: each attempt replaces it with a new one.
    public async Task HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        UserAccount user = await _userQueries.GetAsync(userId, cancellationToken)
            ?? throw new UserNotFoundException(userId);

        if (user.IsDisabled)
        {
            throw new InvalidRequestException("Ο λογαριασμός είναι απενεργοποιημένος. Ενεργοποιήστε τον πρώτα.");
        }

        string password = TemporaryPassword.Generate();
        await _userAccounts.SetPasswordAsync(user.Id, password, cancellationToken);

        _logger.LogInformation("Temporary password set for user {UserId}", user.Id);

        await _accountEmails.SendTemporaryPasswordAsync(user.Email, password, cancellationToken);
    }
}
