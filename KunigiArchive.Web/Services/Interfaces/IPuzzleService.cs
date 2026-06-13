using KunigiArchive.Web.ViewModels.Game;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KunigiArchive.Web.Services.Interfaces;

public interface IPuzzleService
{
    Task<ServiceResponse<PuzzleManagementViewModel>> GetPuzzleManagementAsync(string gameSlug, long gameDayId);
    Task<ServiceResponse<PuzzleCreateViewModel>> PrepareCreateViewModelAsync(string gameSlug, long gameDayId);
    Task<ServiceResponse> CreatePuzzleAsync(PuzzleCreateViewModel viewModel, ModelStateDictionary modelState);
    Task<ServiceResponse<PuzzleEditViewModel>> GetPuzzleEditAsync(long puzzleId);
    Task<ServiceResponse> UpdatePuzzleAsync(PuzzleEditViewModel viewModel, ModelStateDictionary modelState);
    Task<ServiceResponse> DeletePuzzleAsync(long puzzleId);
    Task<ServiceResponse> DeletePuzzleMediaAsync(long puzzleId, long puzzleMediaId);
}