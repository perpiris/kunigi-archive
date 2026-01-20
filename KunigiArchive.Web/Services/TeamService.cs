using System.Security.Claims;
using KunigiArchive.Web.Data;
using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Mappings;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.Utilities;
using KunigiArchive.Web.ViewModels.Account;
using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Team;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Web.Services;

public class TeamService : ITeamService
{
    private readonly ILogger<TeamService> _logger;
    private readonly DataContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDirectoryService _directoryService;
    private readonly IAccountService _accountService;

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User!;

    public TeamService(
        ILogger<TeamService> logger,
        DataContext context,
        IHttpContextAccessor httpContextAccessor,
        IDirectoryService directoryService,
        IAccountService accountService)
    {
        _logger = logger;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _directoryService = directoryService;
        _accountService = accountService;
    }

    public async Task<ServiceResponse<PaginatedViewModel<TeamDetailsViewModel>>> GetPaginatedTeamsAsync(
        short pageNumber = 1, short pageSize = 8, string? searchQuery = null)
    {
        var query = _context.Teams.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(x => x.Name.ToLower().Contains(searchQuery.ToLower()));
        }

        query = query.OrderBy(x => x.Name);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.TeamId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var paginatedData = new PaginatedViewModel<TeamDetailsViewModel>
        {
            Items = [.. items.Select(x => x.MapToTeamDetailsViewModel())],
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            PageSize = pageSize
        };

        return ServiceResponse<PaginatedViewModel<TeamDetailsViewModel>>.Successful(paginatedData);
    }

    public async Task<ServiceResponse> CreateTeamAsync(TeamCreateViewModel viewModel, ModelStateDictionary modelState)
    {
        var slug = SlugGenerator.GenerateSlug(viewModel.Name);

        var nameExists = await _context.Teams.AnyAsync(x => x.Slug == slug);
        if (nameExists)
        {
            modelState.AddModelError("Name", "Υπάρχει ήδη ομάδα με αυτό το όνομα.");
            return ServiceResponse.Failure();
        }

        var newTeam = new Team { Name = viewModel.Name.Trim(), Slug = slug, IsActive = viewModel.IsActive };

        _context.Teams.Add(newTeam);
        await _context.SaveChangesAsync();

        var directoryServiceResponse = _directoryService.CreateDirectory($"teams/{slug}");
        if (!directoryServiceResponse.Success)
        {
            _logger.LogWarning("Failed to create directory for team {TeamSlug}: {Message}", slug,
                directoryServiceResponse.Message);
            return ServiceResponse.Warning("Η ομάδα δημιουργήθηκε με επιτυχία άλλα η δημιουργία του φακέλου απέτυχε.");
        }

        return ServiceResponse.Successful("Η ομάδα δημιουργήθηκε με επιτυχία.");
    }

    public async Task<ServiceResponse<TeamDetailsViewModel>> GetTeamBySlugAsync(string slug, bool includeFullDetails,
        bool isForPublicView = false)
    {
        var query = _context.Teams.AsQueryable();
        if (includeFullDetails)
        {
            query = query
                .Include(x => x.HostedGames)
                .Include(x => x.WonGames)
                .Include(x => x.MediaFiles)
                .AsSplitQuery();
        }

        query = query.Include(x => x.Managers);
        var team = await query.FirstOrDefaultAsync(x => x.Slug == slug);
        if (team == null)
        {
            return ServiceResponse<TeamDetailsViewModel>.Failure("Η ομάδας δεν βρέθηκε.");
        }

        if (!isForPublicView && !HasTeamAccess(team))
        {
            return ServiceResponse<TeamDetailsViewModel>.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτή την ομάδα.");
        }

        return ServiceResponse<TeamDetailsViewModel>.Successful(team.MapToTeamDetailsViewModel(includeFullDetails));
    }

    public async Task<ServiceResponse> UpdateTeamAsync(TeamUpdateViewModel viewModel, IFormFile? image,
        ModelStateDictionary modelState)
    {
        var team = await _context.Teams
            .Include(x => x.Managers)
            .FirstOrDefaultAsync(x => x.Slug == viewModel.Slug);

        if (team is null)
        {
            return ServiceResponse.Failure("Η ομάδας δεν βρέθηκε.");
        }

        if (!HasTeamAccess(team))
        {
            return ServiceResponse<TeamDetailsViewModel>.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτή την ομάδα.");
        }

        team.IsActive = viewModel.IsActive;
        team.YearFounded = viewModel.YearFounded;
        team.Description = viewModel.Description;
        team.Facebook = viewModel.Facebook;
        team.Instagram = viewModel.Instagram;
        team.Youtube = viewModel.Youtube;
        team.Website = viewModel.Website;

        if (User.IsInRole("Admin"))
        {
            team.Name = viewModel.Name;
            team.Slug = viewModel.UpdatedSlug;
        }

        if (!modelState.IsValid)
        {
            return ServiceResponse.Failure();
        }

        if (image is not null)
        {
            var storageDirectory = $"teams/{team.Slug}";
            var saveResult = await _directoryService.SaveFileAsync(image, storageDirectory, "profile");

            if (!saveResult.Success)
            {
                _logger.LogError("Failed to save profile image for team {TeamSlug}: {Message}", team.Slug,
                    saveResult.Message);
                modelState.AddModelError("Image", "Σφάλμα κατά την αποθήκευση της εικόνας.");
                return ServiceResponse.Failure("Σφάλμα κατά την αποθήκευση της εικόνας.");
            }

            team.Image = saveResult.Data!;
        }

        _context.Teams.Update(team);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Η ομάδα ενημερώθηκε με επιτυχία.");
    }

    public async Task<ServiceResponse> DeleteTeamAsync(string slug)
    {
        var team = await _context.Teams
            .Include(x => x.Managers)
            .Include(x => x.MediaFiles)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (team is null)
        {
            return ServiceResponse.Failure("Η ομάδα δεν βρέθηκε.");
        }

        if (team.Managers.Count != 0)
        {
            _context.TeamManagers.RemoveRange(team.Managers);
        }

        if (team.MediaFiles.Count != 0)
        {
            _context.TeamMedia.RemoveRange(team.MediaFiles);
        }

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        var deleteDirectoryResult = _directoryService.DeleteDirectory($"teams/{slug}");
        if (!deleteDirectoryResult.Success)
        {
            _logger.LogWarning("Team deleted from database but failed to delete directory: {Message}",
                deleteDirectoryResult.Message);
            return ServiceResponse.Warning(
                "Η ομάδα διαγράφηκε από τη βάση δεδομένων αλλά η διαγραφή του φακέλου απέτυχε.");
        }

        return ServiceResponse.Successful("Η ομάδα διαγράφηκε με επιτυχία.");
    }

    public async Task<ServiceResponse<TeamManagersViewModel>> GetTeamManagerViewModelAsync(string slug)
    {
        var teamWithManagers = await _context.Teams
            .Include(x => x.Managers)
            .ThenInclude(x => x.AppUser)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (teamWithManagers is null)
        {
            return ServiceResponse<TeamManagersViewModel>.Failure("Η ομάδα δεν βρέθηκε.");
        }

        var currentManagerIds = teamWithManagers.Managers.Select(x => x.AppUserId).ToHashSet();
        var availableUsersServiceResponse = await _accountService.GetAvailableManagersAsync();
        if (!availableUsersServiceResponse.Success)
        {
            return ServiceResponse<TeamManagersViewModel>.Failure("Αποτυχία λήψης διαθέσιμων χρηστών.");
        }

        var availableUserSelectList = availableUsersServiceResponse.Data!
            .Where(x => !currentManagerIds.Contains(x.AppUserId))
            .Select(x => new SelectListItem { Value = x.AppUserId.ToString(), Text = x.Email })
            .ToList();

        availableUserSelectList.Insert(0, new SelectListItem { Value = string.Empty, Text = string.Empty });

        var teamManagerViewModel = new TeamManagersViewModel
        {
            TeamId = teamWithManagers.TeamId,
            TeamName = teamWithManagers.Name,
            Slug = teamWithManagers.Slug,
            AvailableUsers = new SelectList(availableUserSelectList, "Value", "Text"),
            CurrentManagers = teamWithManagers.Managers.Select(x => new AccountDetailsViewModel
            {
                AppUserId = x.AppUserId, Email = x.AppUser.Email!
            }).ToList()
        };

        return ServiceResponse<TeamManagersViewModel>.Successful(teamManagerViewModel);
    }

    public async Task<ServiceResponse> AddTeamManagerAsync(string slug, long appUserId)
    {
        var team = await _context.Teams
            .Include(x => x.Managers)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (team is null)
        {
            return ServiceResponse.Failure("Η ομάδα δεν βρέθηκε.");
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == appUserId);
        if (!userExists)
        {
            return ServiceResponse.Failure("Ο χρήστης δεν βρέθηκε.");
        }

        var managerExists = team.Managers.Any(x => x.AppUserId == appUserId);
        if (managerExists)
        {
            return ServiceResponse.Warning("Ο χρήστης είναι ήδη διαχειριστής της ομάδας.");
        }

        var teamManager = new TeamManager { TeamId = team.TeamId, AppUserId = appUserId };

        _context.TeamManagers.Add(teamManager);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Ο διαχειριστής προστέθηκε με επιτυχία.");
    }

    public async Task<ServiceResponse> RemoveTeamManagerAsync(string slug, long appUserId)
    {
        var team = await _context.Teams
            .Include(x => x.Managers)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (team is null)
        {
            return ServiceResponse.Failure("Η ομάδα δεν βρέθηκε.");
        }

        var teamManager = team.Managers.FirstOrDefault(x => x.AppUserId == appUserId);
        if (teamManager is null)
        {
            return ServiceResponse.Failure("Ο διαχειριστής δεν βρέθηκε.");
        }

        _context.TeamManagers.Remove(teamManager);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Ο διαχειριστής αφαιρέθηκε με επιτυχία.");
    }

    public async Task<ServiceResponse<IEnumerable<TeamDetailsViewModel>>> GetAllUserManagedTeams()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userManagedTeams = await _context.Teams
            .Include(x => x.Managers)
            .Where(x => x.Managers.Any(y => y.AppUserId == userId))
            .ToListAsync();

        return ServiceResponse<IEnumerable<TeamDetailsViewModel>>.Successful(
            userManagedTeams.Select(x => x.MapToTeamDetailsViewModel()));
    }

    public async Task<ServiceResponse<TeamMediaViewModel>> GetTeamMediaViewModelAsync(string slug)
    {
        var team = await _context.Teams
            .Include(x => x.MediaFiles)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (team is null)
        {
            return ServiceResponse<TeamMediaViewModel>.Failure("Η ομάδα δεν βρέθηκε.");
        }

        var teamMediaViewModel = new TeamMediaViewModel
        {
            TeamId = team.TeamId,
            Slug = team.Slug,
            TeamName = team.Name,
            Media = team.MediaFiles.Select(x => x.MapToTeamMediaItemViewModel()).OrderByDescending(x => x.TimeStamp)
                .ToList()
        };

        return ServiceResponse<TeamMediaViewModel>.Successful(teamMediaViewModel);
    }

    public async Task<ServiceResponse> UploadTeamMediaAsync(string slug, List<IFormFile> files)
    {
        var team = await _context.Teams
            .Include(x => x.Managers)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (team is null)
        {
            return ServiceResponse.Failure("Η ομάδα δεν βρέθηκε.");
        }

        if (!HasTeamAccess(team))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα επεξεργασίας αυτής της ομάδας.");
        }

        var storageDirectory = $"teams/{team.Slug}";

        foreach (var file in files)
        {
            var saveResult = await _directoryService.SaveFileAsync(file, storageDirectory);

            if (!saveResult.Success)
            {
                _logger.LogError("Failed to upload file {FileName} for team {TeamSlug}: {Message}", file.FileName, slug,
                    saveResult.Message);
                return ServiceResponse.Failure($"Σφάλμα κατά την αποθήκευση του αρχείου {file.FileName}.");
            }

            var newMedia = new TeamMedia
            {
                TeamId = team.TeamId,
                Path = saveResult.Data!,
                OriginalFileName = file.FileName,
                FileName = Path.GetFileName(saveResult.Data!)
            };
            _context.TeamMedia.Add(newMedia);
        }

        await _context.SaveChangesAsync();
        return ServiceResponse.Successful("Τα αρχεία πολυμέσων ανέβηκαν με επιτυχία.");
    }

    public async Task<ServiceResponse> DeleteTeamMediaAsync(string slug, long teamMediaId)
    {
        var team = await _context.Teams
            .Include(x => x.Managers)
            .Include(x => x.MediaFiles)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (team is null)
        {
            return ServiceResponse.Failure("Η ομάδα δεν βρέθηκε.");
        }

        if (!HasTeamAccess(team))
        {
            return ServiceResponse<TeamDetailsViewModel>.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτή την ομάδα.");
        }

        var media = team.MediaFiles.FirstOrDefault(media => media.TeamMediaId == teamMediaId);
        if (media is null)
        {
            return ServiceResponse.Failure("Το αρχείο πολυμέσων δεν βρέθηκε.");
        }

        var deleteResult = _directoryService.DeleteFile(media.Path);
        if (!deleteResult.Success)
        {
            _logger.LogError("Failed to delete physical file {FilePath} for media ID {MediaId}: {Message}", media.Path,
                media.TeamMediaId, deleteResult.Message);
            return ServiceResponse.Failure("Σφάλμα κατά τη διαγραφή του αρχείου από το δίσκο.");
        }

        _context.TeamMedia.Remove(media);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Το αρχείο πολυμέσων διαγράφηκε με επιτυχία.");
    }

    private bool HasTeamAccess(Team team)
    {
        return User.IsInRole("Admin") ||
               team.Managers.Any(manager =>
                   manager.AppUserId == long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
    }
}