using KunigiArchive.Web.ViewModels.Game;

namespace KunigiArchive.Web.Services.Interfaces;

public interface IPostService
{
    Task<ServiceResponse<PostManagementViewModel>> GetPostManagementAsync(string gameSlug);
    Task<ServiceResponse<PostCreateViewModel>> PrepareCreateViewModelAsync(string gameSlug);
    Task<ServiceResponse> CreatePostAsync(PostCreateViewModel viewModel);
    Task<ServiceResponse<PostEditViewModel>> GetPostEditAsync(long postId);
    Task<ServiceResponse> UpdatePostAsync(PostEditViewModel viewModel);
    Task<ServiceResponse> DeletePostAsync(long postId);
    Task<ServiceResponse<PostMediaViewModel>> GetPostMediaViewModelAsync(string gameSlug, long postId);
    Task<ServiceResponse> UploadPostMediaAsync(string gameSlug, long postId, List<IFormFile> files);
    Task<ServiceResponse> DeletePostMediaAsync(string gameSlug, long postId, long postMediaId);
}