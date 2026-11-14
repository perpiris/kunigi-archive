using KunigiArchive.Application.Teams;
using KunigiArchive.Application.Teams.CreateTeam;
using KunigiArchive.Web2.Contracts.Teams;

namespace KunigiArchive.Web2.Mappers;

public static class TeamMapper
{
    public static CreateTeamCommand ToCommand(this CreateTeamRequest request)
    {
        return new CreateTeamCommand(request.Name, request.IsActive);
    }

    public static TeamResponse ToResponse(this TeamDetails team)
    {
        return new TeamResponse(
            team.Id,
            team.Name,
            team.Slug,
            team.IsActive,
            team.YearFounded,
            team.Description,
            team.Facebook,
            team.Instagram,
            team.Youtube,
            team.Website,
            team.Image);
    }
}