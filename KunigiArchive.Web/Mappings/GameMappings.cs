using KunigiArchive.Web.Entities;
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
            viewModel.Winner = game.Winner.Name;
            viewModel.WinnerId = game.Winner.TeamId;
            viewModel.WinnerSlug = game.Winner.Slug;

            viewModel.Host = game.Host.Name;
            viewModel.HostId = game.Host.TeamId;
            viewModel.HostSlug = game.Host.Slug;

            viewModel.GameDays = game.GameDays
                .Where(gd => gd.IsEnabled)
                .Select(gd => new GameDayViewModel
                {
                    GameDayId = gd.GameDayId, Name = gd.GameDayType.Label, IsEnabled = gd.IsEnabled
                }).ToList();

            // media files
        }

        return viewModel;
    }
}