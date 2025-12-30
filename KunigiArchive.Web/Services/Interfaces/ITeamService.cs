using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Team;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KunigiArchive.Web.Services.Interfaces;

public interface ITeamService
{
    Task<ServiceResponse<PaginatedViewModel<TeamDetailsViewModel>>> GetPaginatedTeamsAsync(short pageNumber = 1,
        short pageSize = 8, string? searchQuery = null);

    Task<ServiceResponse> CreateTeamAsync(TeamCreateViewModel viewModel, ModelStateDictionary modelState);

    Task<ServiceResponse<TeamDetailsViewModel>> GetTeamBySlugAsync(string slug, bool includeFullDetails,
        bool isForPublicView = false);

    Task<ServiceResponse> UpdateTeamAsync(TeamUpdateViewModel viewModel, IFormFile? image,
        ModelStateDictionary modelState);

    Task<ServiceResponse> DeleteTeamAsync(string slug);
    Task<ServiceResponse<TeamManagersViewModel>> GetTeamManagerViewModelAsync(string slug);
    Task<ServiceResponse> AddTeamManagerAsync(string idOrSlug, long appUserId);
    Task<ServiceResponse> RemoveTeamManagerAsync(string idOrSlug, long appUserId);
    Task<ServiceResponse<IEnumerable<TeamDetailsViewModel>>> GetAllUserManagedTeams();
    Task<ServiceResponse<TeamMediaViewModel>> GetTeamMediaViewModelAsync(string slug);
    Task<ServiceResponse> UploadTeamMediaAsync(string slug, List<IFormFile> files);
    Task<ServiceResponse> DeleteTeamMediaAsync(string slug, long teamMediaId);
}