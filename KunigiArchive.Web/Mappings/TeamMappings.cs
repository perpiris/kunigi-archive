using KunigiArchive.Web.Entities;
using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Game;
using KunigiArchive.Web.ViewModels.Team;

namespace KunigiArchive.Web.Mappings;

public static class TeamMappings
{
    public static TeamDetailsViewModel MapToTeamDetailsViewModel(this Team team, bool includeFullDetails = false)
    {
        var viewModel = new TeamDetailsViewModel
        {
            TeamId = team.TeamId,
            Name = team.Name,
            Slug = team.Slug,
            IsActive = team.IsActive,
            YearFounded = team.YearFounded,
            Description = team.Description,
            Facebook = team.Facebook,
            Instagram = team.Instagram,
            Youtube = team.Youtube,
            Website = team.Website,
            Image = team.Image
        };

        if (includeFullDetails)
        {
            viewModel.GamesWon = team.WonGames.Select(x => new GameDetailsViewModel
                {
                    GameId = x.GameId,
                    Slug = x.Slug,
                    OrderTitle = x.OrderTitle,
                    Title = x.Title,
                    Year = x.Year
                })
                .ToList();

            viewModel.GamesHosted = team.HostedGames.Select(x => new GameDetailsViewModel
                {
                    GameId = x.GameId,
                    Slug = x.Slug,
                    OrderTitle = x.OrderTitle,
                    Title = x.Title,
                    Year = x.Year
                })
                .ToList();

            viewModel.Media = team.MediaFiles.Select(x => new MediaFileViewModel
                {
                    MediaFileId = x.TeamMediaId, FileName = x.FileName, Path = x.Path
                })
                .ToList();
        }

        return viewModel;
    }

    public static TeamUpdateViewModel MapToTeamUpdateViewModel(this TeamDetailsViewModel team)
    {
        return new TeamUpdateViewModel
        {
            TeamId = team.TeamId,
            Name = team.Name,
            Slug = team.Slug,
            UpdatedSlug = team.Slug,
            UpdatedName = team.Name,
            IsActive = team.IsActive,
            YearFounded = team.YearFounded,
            Description = team.Description,
            Facebook = team.Facebook,
            Instagram = team.Instagram,
            Youtube = team.Youtube,
            Website = team.Website
        };
    }

    public static TeamMediaItemViewModel MapToTeamMediaItemViewModel(this TeamMedia teamMedia)
    {
        return new TeamMediaItemViewModel
        {
            TeamMediaId = teamMedia.TeamMediaId,
            TeamId = teamMedia.TeamId,
            Path = teamMedia.Path,
            OriginalFileName = teamMedia.OriginalFileName,
            FileName = teamMedia.FileName,
            TimeStamp = teamMedia.TimeStamp
        };
    }
}