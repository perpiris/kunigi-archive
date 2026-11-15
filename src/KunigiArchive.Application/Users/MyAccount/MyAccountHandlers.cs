using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Users.MyAccount;

// The signed-in user's own account: what it is, and changing its email or password. Any role.
public sealed class GetMyAccountHandler
{
    private readonly IUserQueries _userQueries;
    private readonly ICurrentUser _currentUser;

    public GetMyAccountHandler(IUserQueries userQueries, ICurrentUser currentUser)
    {
        _userQueries = userQueries;
        _currentUser = currentUser;
    }

    public async Task<UserAccount> HandleAsync(CancellationToken cancellationToken)
    {
        Guid userId = _currentUser.Id ?? throw new ForbiddenException();
        return await _userQueries.GetAsync(userId, cancellationToken) ?? throw new UserNotFoundException(userId);
    }
}

public sealed record ChangeMyEmailCommand(string CurrentPassword, string NewEmail);

public sealed class ChangeMyEmailHandler
{
    private readonly IUserQueries _userQueries;
    private readonly IUserAccounts _userAccounts;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ChangeMyEmailHandler> _logger;

    public ChangeMyEmailHandler(
        IUserQueries userQueries,
        IUserAccounts userAccounts,
        ICurrentUser currentUser,
        ILogger<ChangeMyEmailHandler> logger)
    {
        _userQueries = userQueries;
        _userAccounts = userAccounts;
        _currentUser = currentUser;
        _logger = logger;
    }

    // Throws InvalidRequestException (wrong password, same or invalid email) or UserEmailAlreadyTakenException.
    public async Task HandleAsync(ChangeMyEmailCommand command, CancellationToken cancellationToken)
    {
        Guid userId = _currentUser.Id ?? throw new ForbiddenException();
        UserAccount user = await _userQueries.GetAsync(userId, cancellationToken) ?? throw new UserNotFoundException(userId);

        string email = UserRules.NormalizeEmail(command.NewEmail);
        if (string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidRequestException("Το νέο email είναι ίδιο με το τωρινό.");
        }

        if (await _userQueries.EmailExistsAsync(email, cancellationToken))
        {
            throw new UserEmailAlreadyTakenException(email);
        }

        await _userAccounts.ChangeEmailAsync(userId, command.CurrentPassword, email, cancellationToken);

        _logger.LogInformation("User {UserId} changed their email", userId);
    }
}

public sealed record ChangeMyPasswordCommand(string CurrentPassword, string NewPassword);

public sealed class ChangeMyPasswordHandler
{
    private readonly IUserAccounts _userAccounts;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ChangeMyPasswordHandler> _logger;

    public ChangeMyPasswordHandler(IUserAccounts userAccounts, ICurrentUser currentUser, ILogger<ChangeMyPasswordHandler> logger)
    {
        _userAccounts = userAccounts;
        _currentUser = currentUser;
        _logger = logger;
    }

    // Throws InvalidRequestException for a wrong current password or a new one that breaks the rules.
    public async Task HandleAsync(ChangeMyPasswordCommand command, CancellationToken cancellationToken)
    {
        Guid userId = _currentUser.Id ?? throw new ForbiddenException();

        await _userAccounts.ChangePasswordAsync(userId, command.CurrentPassword, command.NewPassword, cancellationToken);

        _logger.LogInformation("User {UserId} changed their password", userId);
    }
}