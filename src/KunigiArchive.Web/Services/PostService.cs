using System.Security.Claims;
using KunigiArchive.Web.Data;
using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.ViewModels.Game;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Web.Services;

public class PostService : IPostService
{
    private readonly DataContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDirectoryService _directoryService;

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User!;

    public PostService(DataContext context, IHttpContextAccessor httpContextAccessor, IDirectoryService directoryService)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _directoryService = directoryService;
    }

    public async Task<ServiceResponse<PostManagementViewModel>> GetPostManagementAsync(string gameSlug)
    {
        Game? game = await LoadGameWithAccess(gameSlug);
        if (game == null)
        {
            return ServiceResponse<PostManagementViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse<PostManagementViewModel>.Warning(
                "Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        List<Post> posts = await _context.Posts
            .Where(p => p.GameId == game.GameId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var viewModel = new PostManagementViewModel
        {
            GameId = game.GameId,
            GameSlug = gameSlug,
            GameFormattedTitle = FormatGameTitle(game),
            Posts = posts.Select(p => new PostDetailsViewModel { PostId = p.PostId, GameId = p.GameId, Title = p.Title, Content = p.Content, CreatedAt = p.CreatedAt })
                .ToList()
        };

        return ServiceResponse<PostManagementViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse<PostCreateViewModel>> PrepareCreateViewModelAsync(string gameSlug)
    {
        Game? game = await LoadGameWithAccess(gameSlug);
        if (game == null)
        {
            return ServiceResponse<PostCreateViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse<PostCreateViewModel>.Warning(
                "Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        return ServiceResponse<PostCreateViewModel>.Successful(new PostCreateViewModel { GameId = game.GameId });
    }

    public async Task<ServiceResponse> CreatePostAsync(PostCreateViewModel viewModel)
    {
        Game? game = await _context.Games
            .Include(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(g => g.GameId == viewModel.GameId);

        if (game == null)
        {
            return ServiceResponse.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var post = new Post { GameId = viewModel.GameId, Title = viewModel.Title, Content = viewModel.Content, CreatedAt = DateTime.UtcNow };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Η ανάρτηση δημιουργήθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse<PostEditViewModel>> GetPostEditAsync(long postId)
    {
        Post? post = await _context.Posts
            .Include(p => p.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(p => p.PostId == postId);

        if (post == null)
        {
            return ServiceResponse<PostEditViewModel>.Failure("Η ανάρτηση δεν βρέθηκε.");
        }

        if (!HasGameAccess(post.Game))
        {
            return ServiceResponse<PostEditViewModel>.Warning(
                "Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var viewModel = new PostEditViewModel { PostId = post.PostId, GameId = post.GameId, Title = post.Title, Content = post.Content };

        return ServiceResponse<PostEditViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse> UpdatePostAsync(PostEditViewModel viewModel)
    {
        Post? post = await _context.Posts
            .Include(p => p.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(p => p.PostId == viewModel.PostId);

        if (post == null)
        {
            return ServiceResponse.Failure("Η ανάρτηση δεν βρέθηκε.");
        }

        if (!HasGameAccess(post.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        post.Title = viewModel.Title;
        post.Content = viewModel.Content;
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Η ανάρτηση ενημερώθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse> DeletePostAsync(long postId)
    {
        Post? post = await _context.Posts
            .Include(p => p.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(p => p.PostId == postId);

        if (post == null)
        {
            return ServiceResponse.Failure("Η ανάρτηση δεν βρέθηκε.");
        }

        if (!HasGameAccess(post.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        string storagePath = $"games/{post.Game.Year}/posts/{post.PostId}";

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        _directoryService.DeleteDirectory(storagePath);

        return ServiceResponse.Successful("Η ανάρτηση διαγράφηκε επιτυχώς.");
    }

    public async Task<ServiceResponse<PostMediaViewModel>> GetPostMediaViewModelAsync(string gameSlug, long postId)
    {
        Post? post = await _context.Posts
            .Include(p => p.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .Include(p => p.MediaFiles)
            .FirstOrDefaultAsync(p => p.PostId == postId);

        if (post == null)
        {
            return ServiceResponse<PostMediaViewModel>.Failure("Η ανάρτηση δεν βρέθηκε.");
        }

        if (!HasGameAccess(post.Game))
        {
            return ServiceResponse<PostMediaViewModel>.Warning("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var viewModel = new PostMediaViewModel
        {
            PostId = post.PostId,
            GameId = post.GameId,
            Slug = post.Game.Slug,
            PostTitle = post.Title,
            GameFormattedTitle = FormatGameTitle(post.Game),
            Media = post.MediaFiles.OrderByDescending(m => m.TimeStamp).Select(m => new PostMediaItemViewModel
            {
                PostMediaId = m.PostMediaId,
                Path = m.Path,
                OriginalFileName = m.OriginalFileName,
                FileName = m.FileName,
                TimeStamp = m.TimeStamp
            }).ToList()
        };

        return ServiceResponse<PostMediaViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse> UploadPostMediaAsync(string gameSlug, long postId, List<IFormFile> files)
    {
        Post? post = await _context.Posts
            .Include(p => p.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(p => p.PostId == postId);

        if (post == null)
        {
            return ServiceResponse.Failure("Η ανάρτηση δεν βρέθηκε.");
        }

        if (!HasGameAccess(post.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        string storagePath = $"games/{post.Game.Year}/posts/{post.PostId}";
        int uploadedCount = 0;

        foreach (IFormFile file in files)
        {
            ServiceResponse<string> saveResult = await _directoryService.SaveFileAsync(file, storagePath);
            if (!saveResult.Success || string.IsNullOrEmpty(saveResult.Data)) continue;

            string fileName = saveResult.Data.Split('/').Last();
            var postMedia = new PostMedia
            {
                PostId = post.PostId,
                Path = saveResult.Data,
                OriginalFileName = file.FileName,
                FileName = fileName,
                TimeStamp = DateTime.UtcNow
            };

            _context.PostMedia.Add(postMedia);
            uploadedCount++;
        }

        await _context.SaveChangesAsync();

        return uploadedCount > 0
            ? ServiceResponse.Successful($"Μεταφορτώθηκαν επιτυχώς {uploadedCount} αρχεία.")
            : ServiceResponse.Failure("Δεν ήταν δυνατή η μεταφόρτωση των αρχείων.");
    }

    public async Task<ServiceResponse> DeletePostMediaAsync(string gameSlug, long postId, long postMediaId)
    {
        PostMedia? media = await _context.PostMedia
            .Include(m => m.Post)
            .ThenInclude(p => p.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(m => m.PostMediaId == postMediaId && m.PostId == postId);

        if (media == null)
        {
            return ServiceResponse.Failure("Το αρχείο δεν βρέθηκε.");
        }

        if (!HasGameAccess(media.Post.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        _directoryService.DeleteFile(media.Path);

        _context.PostMedia.Remove(media);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Το αρχείο διεγράφη επιτυχώς.");
    }

    private async Task<Game?> LoadGameWithAccess(string slug)
    {
        return await _context.Games
            .Include(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(g => g.Slug == slug);
    }

    private bool HasGameAccess(Game game)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return game.Host.Managers.Any(m => m.AppUserId == userId);
    }

    private static string FormatGameTitle(Game game)
    {
        string result = $"{game.OrderTitle} {game.Year}";
        if (!string.IsNullOrWhiteSpace(game.Title))
        {
            result += $" - {game.Title}";
        }

        return result;
    }
}