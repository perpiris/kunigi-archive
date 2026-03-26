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

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User!;

    public PostService(DataContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResponse<PostManagementViewModel>> GetPostManagementAsync(string gameSlug)
    {
        var game = await LoadGameWithAccess(gameSlug);
        if (game == null)
        {
            return ServiceResponse<PostManagementViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse<PostManagementViewModel>.Warning(
                "Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var posts = await _context.Posts
            .Where(p => p.GameId == game.GameId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var viewModel = new PostManagementViewModel
        {
            GameId = game.GameId,
            GameSlug = gameSlug,
            GameFormattedTitle = FormatGameTitle(game),
            Posts = posts.Select(p => new PostDetailsViewModel { PostId = p.PostId, GameId = p.GameId, Content = p.Content, CreatedAt = p.CreatedAt })
                .ToList()
        };

        return ServiceResponse<PostManagementViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse<PostCreateViewModel>> PrepareCreateViewModelAsync(string gameSlug)
    {
        var game = await LoadGameWithAccess(gameSlug);
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
        var game = await _context.Games
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

        var post = new Post { GameId = viewModel.GameId, Content = viewModel.Content, CreatedAt = DateTime.UtcNow };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Η ανάρτηση δημιουργήθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse<PostEditViewModel>> GetPostEditAsync(long postId)
    {
        var post = await _context.Posts
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

        var viewModel = new PostEditViewModel { PostId = post.PostId, GameId = post.GameId, Content = post.Content };

        return ServiceResponse<PostEditViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse> UpdatePostAsync(PostEditViewModel viewModel)
    {
        var post = await _context.Posts
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

        post.Content = viewModel.Content;
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Η ανάρτηση ενημερώθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse> DeletePostAsync(long postId)
    {
        var post = await _context.Posts
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

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Η ανάρτηση διαγράφηκε επιτυχώς.");
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

        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return game.Host.Managers.Any(m => m.AppUserId == userId);
    }

    private static string FormatGameTitle(Game game)
    {
        var result = $"{game.OrderTitle} {game.Year}";
        if (!string.IsNullOrWhiteSpace(game.Title))
        {
            result += $" - {game.Title}";
        }

        return result;
    }
}