using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Teams;

namespace KunigiArchive.Application.Mappers;

public static class TeamMapper
{
    public static TeamDetails ToDetails(this Team team)
    {
        return new TeamDetails(
            team.Id,
            team.Name,
            team.Slug.Value,
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