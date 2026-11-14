using KunigiArchive.Web.Entities;
using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Game;

namespace KunigiArchive.Web.Mappings;

public static class GameMappings
{
    public static GameDetailsViewModel MapToGameDetailsViewModel(this Game game, bool includeFullDetails = false)
    {
        var viewModel = new GameDetailsViewModel
        {
            GameId = game.GameId,
            Slug = game.Slug,
            Year = game.Year,
            Order = game.Order,
            OrderTitle = game.OrderTitle,
            Title = game.Title,
            Description = game.Description,
            Image = game.Image
        };

        if (includeFullDetails)
        {
            viewModel.Winner = game.Winner?.Name;
            viewModel.WinnerId = game.WinnerId;
            viewModel.WinnerSlug = game.Winner?.Slug;

            viewModel.Host = game.Host.Name;
            viewModel.HostId = game.Host.TeamId;
            viewModel.HostSlug = game.Host.Slug;

            viewModel.GameDays = game.GameDays
                .Where(gd => gd.IsEnabled)
                .Select(gd => new GameDayViewModel
                {
                    GameDayId = gd.GameDayId,
                    Name = gd.GameDayType.Label,
                    IsEnabled = gd.IsEnabled
                }).ToList();

            viewModel.Media = game.MediaFiles.Select(m => new MediaFileViewModel { MediaFileId = m.GameMediaId, FileName = m.FileName, Path = m.Path })
                .ToList();

            viewModel.Posts = game.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostDetailsViewModel
                {
                    PostId = p.PostId,
                    GameId = p.GameId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    Media = p.MediaFiles.Select(m => new MediaFileViewModel { MediaFileId = m.PostMediaId, FileName = m.FileName, Path = m.Path }).ToList()
                }).ToList();
        }

        return viewModel;
    }
}