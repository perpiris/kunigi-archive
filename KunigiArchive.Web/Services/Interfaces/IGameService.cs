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

    Task<ServiceResponse<GameDetailsViewModel>> GetGameBySlugAsync(string slug, bool includeFullDetails,
        bool isForPublicView = false);

    Task<ServiceResponse<GameDaysViewModel>> GetGameDaysViewModelAsync(string slug);

    Task<ServiceResponse<GameDayActionsViewModel>> GetGameDayActionsAsync(string slug, long gameDayId);

    Task<ServiceResponse> UpdateGameDaysAsync(string slug, List<long> enabledDayIds);

    Task<ServiceResponse<GameUpdateViewModel>> PrepareGameUpdateViewModelAsync(string slug,
        GameUpdateViewModel? gameUpdateViewModel = null);

    Task<ServiceResponse<string>> UpdateGameAsync(GameUpdateViewModel gameUpdateViewModel, IFormFile? image,
        ModelStateDictionary modelState);

    Task<ServiceResponse<GameMediaViewModel>> GetGameMediaViewModelAsync(string slug);
    Task<ServiceResponse> UploadGameMediaAsync(string slug, List<IFormFile> files);
    Task<ServiceResponse> DeleteGameMediaAsync(string slug, long gameMediaId);

    Task<ServiceResponse<GameDayMediaViewModel>> GetGameDayMediaViewModelAsync(string slug, long gameDayId);
    Task<ServiceResponse> UploadGameDayMediaAsync(string slug, long gameDayId, List<IFormFile> files);
    Task<ServiceResponse> DeleteGameDayMediaAsync(string slug, long gameDayId, long gameDayMediaId);
}