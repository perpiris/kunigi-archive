using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Common;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Teams.UpdateTeam;

public sealed class UpdateTeamHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IRichTextSanitizer _sanitizer;
    private readonly ISlugHistory _slugHistory;
    private readonly ILogger<UpdateTeamHandler> _logger;

    public UpdateTeamHandler(
        ITeamRepository teamRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IRichTextSanitizer sanitizer,
        ISlugHistory slugHistory,
        ILogger<UpdateTeamHandler> logger)
    {
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
        _slugHistory = slugHistory;
        _logger = logger;
    }

    public async Task<TeamDetails> HandleAsync(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        Team team = await _teamRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new TeamNotFoundException(command.Id.ToString());

        _currentUser.EnsureCanEdit(team);

        string previousName = team.Name;

        // The archive keeps a team's name; renames are rare corrections, so only admins make them.
        // Teams that split or start over are created as new teams.
        if (command.Name.Trim() != previousName)
        {
            _currentUser.EnsureAdmin();
        }

        team.Update(
            command.Name,
            command.IsActive,
            command.YearFounded,
            _sanitizer.Sanitize(command.Description),
            command.Facebook,
            command.Instagram,
            command.Youtube,
            command.Website);

        if (team.Name != previousName && await _teamRepository.NameExistsAsync(team.Name, team.Id, cancellationToken))
        {
            throw new TeamNameAlreadyTakenException(team.Name);
        }

        // An empty field keeps the current slug (#82).
        if (!string.IsNullOrWhiteSpace(command.Slug))
        {
            await ChangeSlugAsync(team, Slug.FromName(command.Slug), cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Team {TeamId} updated", team.Id);

        return team.ToDetails();
    }

    private async Task ChangeSlugAsync(Team team, Slug slug, CancellationToken cancellationToken)
    {
        if (slug == team.Slug)
        {
            return;
        }

        _currentUser.EnsureAdmin();

        if (await _teamRepository.SlugExistsAsync(slug, cancellationToken))
        {
            throw new SlugAlreadyTakenException(slug);
        }

        // The old address keeps working: it redirects to the new one.
        await _slugHistory.RecordAsync(SlugOwner.Team, team.Slug.Value, team.Id, cancellationToken);
        team.ChangeSlug(slug);
    }
}