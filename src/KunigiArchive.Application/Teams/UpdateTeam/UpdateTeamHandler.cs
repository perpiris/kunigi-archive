using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Teams.UpdateTeam;

public sealed class UpdateTeamHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTeamHandler> _logger;

    public UpdateTeamHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork, ILogger<UpdateTeamHandler> logger)
    {
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TeamDetails> HandleAsync(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        Team team = await _teamRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new TeamNotFoundException(command.Id.ToString());

        Slug previousSlug = team.Slug;

        team.Update(
            command.Name,
            command.IsActive,
            command.YearFounded,
            command.Description,
            command.Facebook,
            command.Instagram,
            command.Youtube,
            command.Website);

        if (team.Slug != previousSlug && await _teamRepository.SlugExistsAsync(team.Slug, cancellationToken))
        {
            throw new TeamNameAlreadyTakenException(team.Name);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Team {TeamId} updated", team.Id);

        return team.ToDetails();
    }
}