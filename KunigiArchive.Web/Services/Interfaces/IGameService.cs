using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Game;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KunigiArchive.Web.Services.Interfaces;

public interface IGameService
{
    Task<ServiceResponse<PaginatedViewModel<GameDetailsViewModel>>> GetPaginatedGamesAsync(short pageNumber = 1,
        short pageSize = 8, string? searchQuery = null);

    Task<ServiceResponse<GameCreateViewModel>> PrepareGameCreateViewModelViewModel(
        GameCreateViewModel? gameCreateViewModel = null);

    Task<ServiceResponse> CreateGameAsync(GameCreateViewModel gameCreateViewModel, ModelStateDictionary modelState);

    Task<ServiceResponse<GameDetailsViewModel>> GetGameBySlugAsync(string slug, bool includeFullDetails);

    Task<ServiceResponse<GameDaysViewModel>> GetGameDaysViewModelAsync(string slug);

    Task<ServiceResponse> UpdateGameDaysAsync(string slug, List<long> enabledDayIds);

    Task<ServiceResponse<GameUpdateViewModel>> PrepareGameUpdateViewModelAsync(string slug,
        GameUpdateViewModel? gameUpdateViewModel = null);

    Task<ServiceResponse<string>> UpdateGameAsync(GameUpdateViewModel gameUpdateViewModel, IFormFile? image,
        ModelStateDictionary modelState);
}