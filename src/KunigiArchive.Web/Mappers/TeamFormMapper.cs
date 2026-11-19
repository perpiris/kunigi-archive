using KunigiArchive.Application.Teams;
using KunigiArchive.Application.Teams.CreateTeam;
using KunigiArchive.Application.Teams.UpdateTeam;
using KunigiArchive.Web.Forms.Teams;

namespace KunigiArchive.Web.Mappers;

public static class TeamFormMapper
{
    public static CreateTeamCommand ToCommand(this CreateTeamForm form)
    {
        return new CreateTeamCommand(form.Name, form.IsActive);
    }

    public static UpdateTeamCommand ToCommand(this EditTeamForm form)
    {
        return new UpdateTeamCommand(
            form.Id,
            form.Name,
            form.Slug,
            form.IsActive,
            form.YearFounded,
            form.Description,
            form.Facebook,
            form.Instagram,
            form.Youtube,
            form.Website);
    }

    public static EditTeamForm ToEditForm(this TeamDetails team)
    {
        return new EditTeamForm
        {
            Id = team.Id,
            Name = team.Name,
            Slug = team.Slug,
            IsActive = team.IsActive,
            YearFounded = team.YearFounded,
            Description = team.Description,
            Facebook = team.Facebook,
            Instagram = team.Instagram,
            Youtube = team.Youtube,
            Website = team.Website
        };
    }
}