using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Users.ChangeUserRole;

public sealed class ChangeUserRoleHandler
{
    private readonly IUserQueries _userQueries;
    private readonly IUserAccounts _userAccounts;
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ChangeUserRoleHandler> _logger;

    public ChangeUserRoleHandler(
        IUserQueries userQueries,
        IUserAccounts userAccounts,
        ITeamRepository teamRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<ChangeUserRoleHandler> logger)
    {
        _userQueries = userQueries;
        _userAccounts = userAccounts;
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        string? role = UserRules.ValidateRole(command.Role);

        // Also guarantees there's always at least one admin: the last one can't demote themselves.
        if (command.UserId == _currentUser.Id)
        {
            throw new InvalidRequestException("Δεν μπορείτε να αλλάξετε τον ρόλο του δικού σας λογαριασμού.");
        }

        UserAccount user = await _userQueries.GetAsync(command.UserId, cancellationToken)
            ?? throw new UserNotFoundException(command.UserId);

        if (user.Role == role)
        {
            return;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Only managers can be assigned to teams, so a former manager leaves all of them.
            if (user.Role == Roles.Manager)
            {
                foreach (Team team in await _teamRepository.GetManagedByAsync(user.Id, ct))
                {
                    team.RemoveManager(user.Id);
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }

            await _userAccounts.SetRoleAsync(user.Id, role, ct);
        }, cancellationToken);

        _logger.LogInformation("User {UserId} role changed from {OldRole} to {NewRole}",
            user.Id, user.Role ?? "(none)", role ?? "(none)");
    }
}
