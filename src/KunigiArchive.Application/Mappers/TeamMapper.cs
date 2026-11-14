using KunigiArchive.Application.Teams;
using KunigiArchive.Application.Users;
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
            team.Image,
            team.Media.OrderByDescending(m => m.UploadedAt).Select(ToDetails).ToList());
    }

    public static TeamMediaDetails ToDetails(this TeamMedia media)
    {
        return new TeamMediaDetails(media.Id, media.Path, media.OriginalFileName, media.UploadedAt);
    }

    public static TeamManagersDetails ToManagersDetails(
        this Team team,
        IReadOnlyList<UserSummary> managers,
        IReadOnlyList<UserSummary> availableManagers)
    {
        return new TeamManagersDetails(team.Id, team.Name, team.Slug.Value, managers, availableManagers);
    }

    public static TeamSummary ToSummary(this Team team)
    {
        return new TeamSummary(team.Id, team.Name, team.Slug.Value, team.IsActive, team.Image);
    }
}