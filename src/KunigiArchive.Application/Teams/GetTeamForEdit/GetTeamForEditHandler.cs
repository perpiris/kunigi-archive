using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Teams;

namespace KunigiArchive.Application.Teams.GetTeamForEdit;

public sealed class GetTeamForEditHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICurrentUser _currentUser;

    public GetTeamForEditHandler(ITeamRepository teamRepository, ICurrentUser currentUser)
    {
        _teamRepository = teamRepository;
        _currentUser = currentUser;
    }

    public async Task<TeamDetails> HandleAsync(string slug, CancellationToken cancellationToken)
    {
        if (!Slug.TryParse(slug, out Slug? parsedSlug))
        {
            throw new TeamNotFoundException(slug);
        }

        Team team = await _teamRepository.GetBySlugAsync(parsedSlug, cancellationToken)
            ?? throw new TeamNotFoundException(slug);

        _currentUser.EnsureCanEdit(team);

        return team.ToDetails();
    }
}