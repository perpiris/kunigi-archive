using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Users.CreateUser;

public sealed class CreateUserHandler
{
    private readonly IUserQueries _userQueries;
    private readonly IUserAccounts _userAccounts;
    private readonly IAccountEmails _accountEmails;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(
        IUserQueries userQueries,
        IUserAccounts userAccounts,
        IAccountEmails accountEmails,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateUserHandler> logger)
    {
        _userQueries = userQueries;
        _userAccounts = userAccounts;
        _accountEmails = accountEmails;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        string email = UserRules.NormalizeEmail(command.Email);
        string? role = UserRules.ValidateRole(command.Role);

        if (await _userQueries.EmailExistsAsync(email, cancellationToken))
        {
            throw new UserEmailAlreadyTakenException(email);
        }

        string password = TemporaryPassword.Generate();

        // User and role together: a failed role assignment must not leave a user behind.
        Guid userId = Guid.Empty;
        await _unitOfWork.ExecuteInTransactionAsync(
            async ct => userId = await _userAccounts.CreateAsync(email, password, role, ct),
            cancellationToken);

        _logger.LogInformation("User {UserId} created with role {Role}", userId, role ?? "(none)");

        // Queued, sent in the background (#20). If it keeps failing, the admin can send a new temporary password.
        await _accountEmails.SendAccountCreatedAsync(email, password, cancellationToken);
        return new CreateUserResult(userId);
    }
}
