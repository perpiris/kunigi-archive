using KunigiArchive.Application.Teams;
using KunigiArchive.Application.Teams.AssignTeamManager;
using KunigiArchive.Application.Teams.CreateTeam;
using KunigiArchive.Application.Teams.GetTeams;
using KunigiArchive.Application.Teams.UpdateTeam;
using KunigiArchive.Web2.Contracts.Teams;

namespace KunigiArchive.Web2.Mappers;

public static class TeamMapper
{
    public static CreateTeamCommand ToCommand(this CreateTeamRequest request)
    {
        return new CreateTeamCommand(request.Name, request.IsActive);
    }

    public static UpdateTeamCommand ToCommand(this UpdateTeamRequest request, Guid id)
    {
        return new UpdateTeamCommand(
            id,
            request.Name,
            request.Slug,
            request.IsActive,
            request.YearFounded,
            request.Description,
            request.Facebook,
            request.Instagram,
            request.Youtube,
            request.Website);
    }

    public static AssignTeamManagerCommand ToCommand(this AssignTeamManagerRequest request, Guid teamId)
    {
        return new AssignTeamManagerCommand(teamId, request.UserId);
    }

    public static GetTeamsQuery ToQuery(this GetTeamsRequest request)
    {
        TeamsCursor.TryDecode(request.After, out TeamsCursor? after);
        return new GetTeamsQuery(after, request.Search);
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
            team.Image,
            team.Media.Select(ToResponse).ToList());
    }

    public static TeamMediaResponse ToResponse(this TeamMediaDetails media)
    {
        return new TeamMediaResponse(media.Id, media.Path, media.ThumbnailPath, media.OriginalFileName, media.UploadedAt);
    }

    public static TeamsPageResponse ToResponse(this TeamsPage page)
    {
        return new TeamsPageResponse(page.Items.Select(ToResponse).ToList(), page.NextCursor);
    }

    public static TeamSummaryResponse ToResponse(this TeamSummary team)
    {
        return new TeamSummaryResponse(team.Id, team.Name, team.Slug, team.IsActive, team.Image);
    }
}