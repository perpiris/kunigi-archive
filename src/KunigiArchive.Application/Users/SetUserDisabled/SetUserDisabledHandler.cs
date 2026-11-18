using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Authorization;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Users.SetUserDisabled;

// Disabling keeps the account (and its team assignments) but stops it from signing in or resetting its
// password; its open sessions end within a minute. Enabling undoes that (#24).
public sealed class SetUserDisabledHandler
{
    private readonly IUserQueries _userQueries;
    private readonly IUserAccounts _userAccounts;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SetUserDisabledHandler> _logger;

    public SetUserDisabledHandler(
        IUserQueries userQueries,
        IUserAccounts userAccounts,
        ICurrentUser currentUser,
        ILogger<SetUserDisabledHandler> logger)
    {
        _userQueries = userQueries;
        _userAccounts = userAccounts;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(Guid userId, bool disabled, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        // Also keeps at least one admin able to sign in.
        if (userId == _currentUser.Id)
        {
            throw new InvalidRequestException("Δεν μπορείτε να απενεργοποιήσετε τον δικό σας λογαριασμό.");
        }

        UserAccount user = await _userQueries.GetAsync(userId, cancellationToken)
            ?? throw new UserNotFoundException(userId);

        if (user.IsDisabled == disabled)
        {
            return;
        }

        await _userAccounts.SetDisabledAsync(user.Id, disabled, cancellationToken);

        _logger.LogInformation("User {UserId} {Action}", user.Id, disabled ? "disabled" : "enabled");
    }
}
