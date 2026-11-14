using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Teams.CreateTeam;

public sealed class CreateTeamHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateTeamHandler> _logger;

    public CreateTeamHandler(
        ITeamRepository teamRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateTeamHandler> logger)
    {
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TeamDetails> HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        Team team = Team.Create(command.Name, command.IsActive);

        // The slug is what has to be unique: two different names can produce the same URL.
        if (await _teamRepository.SlugExistsAsync(team.Slug, cancellationToken))
        {
            throw new TeamNameAlreadyTakenException(team.Name);
        }

        _teamRepository.Add(team);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Team {TeamId} created with slug {TeamSlug}", team.Id, team.Slug.Value);

        return team.ToDetails();
    }
}