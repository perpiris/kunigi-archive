using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Authorization;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Users.DeleteUser;

// Removes the account for good, with its team-manager assignments. Nothing else in the archive points to a
// user, so teams, games and posts stay as they are (#24).
public sealed class DeleteUserHandler
{
    private readonly IUserQueries _userQueries;
    private readonly IUserAccounts _userAccounts;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteUserHandler> _logger;

    public DeleteUserHandler(
        IUserQueries userQueries,
        IUserAccounts userAccounts,
        ICurrentUser currentUser,
        ILogger<DeleteUserHandler> logger)
    {
        _userQueries = userQueries;
        _userAccounts = userAccounts;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        if (userId == _currentUser.Id)
        {
            throw new InvalidRequestException("Δεν μπορείτε να διαγράψετε τον δικό σας λογαριασμό.");
        }

        UserAccount user = await _userQueries.GetAsync(userId, cancellationToken)
            ?? throw new UserNotFoundException(userId);

        await _userAccounts.DeleteAsync(user.Id, cancellationToken);

        _logger.LogInformation("User {UserId} deleted", user.Id);
    }
}
