using System.Reflection;
using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Common;
using KunigiArchive.Application.GameDays;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Media;
using KunigiArchive.Application.Posts;
using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.GameDays;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Posts;
using KunigiArchive.Domain.Teams;
using KunigiArchive.Infrastructure.Identity;
using KunigiArchive.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KunigiArchive.Import;

// Copies the old site into Web2's database and media folder, replacing whatever content is there (teams, games
// and everything under them). Users are matched by email and updated, never deleted. Every row goes through the
// domain (the same rules as the pages), every file through Web2's upload pipeline (shrunk WebP, no EXIF/GPS).
// Without --apply nothing is written: the same checks run and the report says what an import would do.
internal sealed class ImportPipeline
{
    private readonly KunigiArchiveDbContext _dbContext;
    private readonly IMediaStorage _mediaStorage;
    private readonly IRichTextSanitizer _sanitizer;
    private readonly ISlugHistory _slugHistory;
    private readonly ImportReport _report;
    private readonly ImportSettings _settings;

    private readonly HashSet<string> _referencedFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<long, Guid> _userIds = [];
    private readonly Dictionary<long, Guid> _teamIds = [];
    private readonly Dictionary<long, Guid> _gameIds = [];
    private IdMap _idMap = null!;

    public ImportPipeline(
        KunigiArchiveDbContext dbContext,
        IMediaStorage mediaStorage,
        IRichTextSanitizer sanitizer,
        ISlugHistory slugHistory,
        ImportReport report,
        ImportSettings settings)
    {
        _dbContext = dbContext;
        _mediaStorage = mediaStorage;
        _sanitizer = sanitizer;
        _slugHistory = slugHistory;
        _report = report;
        _settings = settings;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        SourceData source = await SourceReader.ReadAsync(_settings.SourceConnectionString, cancellationToken);
        _report.Info($"Read {source.Users.Count} users, {source.Teams.Count} teams, {source.Games.Count} games, "
            + $"{source.GameDays.Count} game days, {source.Puzzles.Sum(g => g.Count())} puzzles, {source.Posts.Count} posts.");

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _idMap = await IdMap.LoadAsync(_dbContext, cancellationToken);
        await ClearContentAsync(cancellationToken);

        await ImportUsersAsync(source, cancellationToken);
        await ImportTeamsAsync(source, cancellationToken);
        await ImportGamesAsync(source, cancellationToken);
        await ImportGameDaysAsync(source, cancellationToken);
        await ImportPostsAsync(source, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _idMap.SaveAsync(_dbContext, cancellationToken);

        ReportUnreferencedFiles();

        if (_settings.Apply)
        {
            await transaction.CommitAsync(cancellationToken);
            _report.Info("Committed.");
        }
        else
        {
            await transaction.RollbackAsync(cancellationToken);
            _report.Info("Dry run: nothing was written. Run with --apply to import.");
        }
    }

    // The content tables, children first. Users stay (they are matched by email). The media folders go too, so a
    // run never leaves files from the previous one behind.
    private async Task ClearContentAsync(CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM PuzzleMedia; DELETE FROM Puzzles; DELETE FROM GameDayMedia; DELETE FROM GameDays;
            DELETE FROM PostMedia; DELETE FROM Posts; DELETE FROM GameMedia; DELETE FROM Games;
            DELETE FROM TeamMedia; DELETE FROM TeamManagers; DELETE FROM Teams; DELETE FROM SlugRedirects;
            """,
            cancellationToken);

        if (_settings.Apply)
        {
            _mediaStorage.DeleteDirectory("teams");
            _mediaStorage.DeleteDirectory("games");
        }
    }

    // ---------- Users ----------

    private async Task ImportUsersAsync(SourceData source, CancellationToken cancellationToken)
    {
        Dictionary<string, Guid> roleIds = await _dbContext.Roles
            .Where(r => r.Name != null)
            .ToDictionaryAsync(r => r.Name!, r => r.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (SourceUser old in source.Users)
        {
            string what = $"User {old.Id} ({old.Email})";
            if (string.IsNullOrWhiteSpace(old.Email))
            {
                _report.Skipped(what, "has no email.");
                continue;
            }

            string normalizedEmail = old.NormalizedEmail ?? old.Email.ToUpperInvariant();
            string userName = old.UserName ?? old.Email;
            string normalizedUserName = old.NormalizedUserName ?? userName.ToUpperInvariant();

            AppUser? user = _idMap.TryGet("Users", old.Id, out Guid mappedId)
                ? await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == mappedId, cancellationToken)
                : null;
            user ??= await _dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

            if (user is null)
            {
                user = new AppUser { Id = _idMap.For("Users", old.Id) };
                _dbContext.Users.Add(user);
            }
            else
            {
                _idMap.Set("Users", old.Id, user.Id);
            }

            if (await _dbContext.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName && u.Id != user.Id, cancellationToken))
            {
                _report.Skipped(what, "another user of the new site has the same user name.");
                if (_dbContext.Entry(user).State == EntityState.Added)
                {
                    _dbContext.Users.Remove(user);
                }

                continue;
            }

            // The password hash and security stamp come along, so everyone signs in with the password they have.
            user.UserName = userName;
            user.NormalizedUserName = normalizedUserName;
            user.Email = old.Email;
            user.NormalizedEmail = normalizedEmail;
            user.EmailConfirmed = old.EmailConfirmed;
            user.PasswordHash = old.PasswordHash;
            user.SecurityStamp = old.SecurityStamp ?? Guid.NewGuid().ToString("N");
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            user.PhoneNumber = old.PhoneNumber;
            user.LockoutEnabled = old.LockoutEnabled;
            user.LockoutEnd = old.LockoutEnd;

            List<IdentityUserRole<Guid>> currentRoles =
                await _dbContext.UserRoles.Where(r => r.UserId == user.Id).ToListAsync(cancellationToken);
            _dbContext.UserRoles.RemoveRange(currentRoles);
            foreach (string role in old.Roles)
            {
                if (roleIds.TryGetValue(role, out Guid roleId))
                {
                    _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roleId });
                }
                else
                {
                    _report.Changed(what, $"role '{role}' doesn't exist in the new site and was left out.");
                }
            }

            _userIds[old.Id] = user.Id;
        }
    }

    // ---------- Teams ----------

    private async Task ImportTeamsAsync(SourceData source, CancellationToken cancellationToken)
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> slugs = new(StringComparer.Ordinal);

        foreach (SourceTeam old in source.Teams)
        {
            string what = $"Team {old.Id} '{old.Name}'";
            // An empty name stays empty, and Team.Create refuses it below.
            string name = Truncate(old.Name, Team.NameMaxLength, what, "name") ?? string.Empty;
            if (!names.Add(name))
            {
                string unique = $"{name} ({old.Id})";
                _report.Changed(what, $"name already taken by another team; renamed to '{unique}'.");
                name = unique;
                names.Add(name);
            }

            Team team;
            try
            {
                team = Team.Create(name, old.IsActive);
            }
            catch (DomainException exception)
            {
                _report.Skipped(what, exception.Message);
                continue;
            }

            SetId(team, _idMap.For("Teams", old.Id));

            short? yearFounded = old.YearFounded;
            if (yearFounded is not null && yearFounded < Team.YearFoundedMin)
            {
                _report.Changed(what, $"year founded {yearFounded} is before {Team.YearFoundedMin}; left empty.");
                yearFounded = null;
            }

            team.Update(
                name,
                old.IsActive,
                yearFounded,
                _sanitizer.Sanitize(old.Description),
                Link(old.Facebook, what, "Facebook"),
                Link(old.Instagram, what, "Instagram"),
                Link(old.Youtube, what, "YouTube"),
                Link(old.Website, what, "website"));

            // The old address is kept when it's still a valid one; otherwise it redirects to the new one.
            if (Slug.TryParse(old.Slug, out Slug? oldSlug) && !slugs.Contains(oldSlug.Value))
            {
                team.ChangeSlug(oldSlug);
            }
            else if (slugs.Contains(team.Slug.Value))
            {
                team.ChangeSlug(Slug.FromName($"{name} {old.Id}"));
            }

            if (team.Slug.Value != old.Slug)
            {
                _report.Changed(what, $"address /teams/{old.Slug} is now /teams/{team.Slug.Value} (the old one redirects).");
                await _slugHistory.RecordAsync(SlugOwner.Team, old.Slug, team.Id, cancellationToken);
            }

            slugs.Add(team.Slug.Value);
            _dbContext.Teams.Add(team);

            string directory = TeamMediaPaths.Directory(team.Id);
            StoredMedia? image = await CopyAsync(old.Image, directory, $"{what} picture", cancellationToken);
            if (image is not null)
            {
                team.SetImage(image.Path);
            }

            foreach (SourceMedia media in source.TeamMedia[old.Id])
            {
                StoredMedia? stored = await CopyAsync(media.Path, directory, $"{what} media {media.Id}", cancellationToken);
                if (stored is not null)
                {
                    TeamMedia added = team.AddMedia(stored.Path, stored.FileName, OriginalName(media), Utc(media.TimeStamp));
                    SetId(added, _idMap.For("TeamMedia", media.Id));
                }
            }

            foreach (long oldUserId in source.TeamManagers[old.Id])
            {
                if (_userIds.TryGetValue(oldUserId, out Guid userId))
                {
                    team.AssignManager(userId);
                }
                else
                {
                    _report.Changed(what, $"manager (old user {oldUserId}) wasn't imported; left out.");
                }
            }

            _teamIds[old.Id] = team.Id;
        }
    }

    // ---------- Games ----------

    private async Task ImportGamesAsync(SourceData source, CancellationToken cancellationToken)
    {
        HashSet<short> years = [];
        HashSet<decimal> orders = [];

        foreach (SourceGame old in source.Games)
        {
            string what = $"Game {old.Id} ({old.Year}, {old.Order})";
            if (!years.Add(old.Year))
            {
                _report.Skipped(what, $"another game has the year {old.Year} (Web2 allows one game per year).");
                continue;
            }

            // The old app needed a number for every game; 0 or less stands for an unnumbered one.
            decimal? order = old.Order > 0 ? old.Order : null;
            if (order is not null && !orders.Add(order.Value))
            {
                _report.Changed(what, $"number {order} is taken by another game; imported without a number.");
                order = null;
            }

            Guid? hostId = TeamId(old.HostId, what, "host");
            Guid? winnerId = old.WinnerId is long winner ? TeamId(winner, what, "winner") : null;

            Game game;
            try
            {
                game = Game.Create(old.Year, order, hostId, winnerId);
                game.UpdateDetails(Truncate(old.Title, Game.TitleMaxLength, what, "title"), null, _sanitizer.Sanitize(old.Description));
            }
            catch (DomainException exception)
            {
                _report.Skipped(what, exception.Message);
                continue;
            }

            SetId(game, _idMap.For("Games", old.Id));
            _dbContext.Games.Add(game);

            if (game.Slug.Value != old.Slug)
            {
                _report.Changed(what, $"address /games/{old.Slug} is now /games/{game.Slug.Value} (the old one redirects).");
                await _slugHistory.RecordAsync(SlugOwner.Game, old.Slug, game.Id, cancellationToken);
            }

            string directory = GameMediaPaths.Directory(game.Id);
            StoredMedia? poster = await CopyAsync(old.Image, directory, $"{what} poster", cancellationToken);
            if (poster is not null)
            {
                game.SetImage(poster.Path);
            }

            foreach (SourceMedia media in source.GameMedia[old.Id])
            {
                StoredMedia? stored = await CopyAsync(media.Path, directory, $"{what} media {media.Id}", cancellationToken);
                if (stored is not null)
                {
                    GameMedia added = game.AddMedia(stored.Path, stored.FileName, OriginalName(media), Utc(media.TimeStamp));
                    SetId(added, _idMap.For("GameMedia", media.Id));
                }
            }

            _gameIds[old.Id] = game.Id;
        }
    }

    // ---------- Game days and puzzles ----------

    private async Task ImportGameDaysAsync(SourceData source, CancellationToken cancellationToken)
    {
        HashSet<(Guid GameId, GameDayType Type)> days = [];

        foreach (SourceGameDay old in source.GameDays)
        {
            string what = $"Game day {old.Id} ({old.TypeLabel}, game {old.GameId})";
            if (!_gameIds.TryGetValue(old.GameId, out Guid gameId))
            {
                _report.Skipped(what, "its game wasn't imported.");
                continue;
            }

            if (DayType(old.TypeLabel) is not GameDayType type)
            {
                _report.Skipped(what, $"unknown kind of day '{old.TypeLabel}'.");
                continue;
            }

            if (!days.Add((gameId, type)))
            {
                _report.Skipped(what, $"the game already has a '{type.Label()}' day.");
                continue;
            }

            GameDay day = GameDay.Create(gameId, type);
            SetId(day, _idMap.For("GameDays", old.Id));
            day.SetEnabled(old.IsEnabled);
            day.UpdateDetails(Truncate(old.Title, GameDay.TitleMaxLength, what, "title"), _sanitizer.Sanitize(old.Description));
            _dbContext.GameDays.Add(day);

            string directory = GameDayPaths.Directory(gameId, day.Id);

            // Web2 days have no picture of their own: the old one joins the day's media.
            StoredMedia? image = await CopyAsync(old.Image, directory, $"{what} picture", cancellationToken);
            if (image is not null)
            {
                day.AddMedia(image.Path, image.FileName, Path.GetFileName(old.Image!), DateTimeOffset.UtcNow);
                _report.Changed(what, "its picture was added to the day's media.");
            }

            foreach (SourceMedia media in source.GameDayMedia[old.Id])
            {
                StoredMedia? stored = await CopyAsync(media.Path, directory, $"{what} media {media.Id}", cancellationToken);
                if (stored is not null)
                {
                    GameDayMedia added = day.AddMedia(stored.Path, stored.FileName, OriginalName(media), Utc(media.TimeStamp));
                    SetId(added, _idMap.For("GameDayMedia", media.Id));
                }
            }

            await ImportPuzzlesAsync(source, old, day, what, cancellationToken);
        }
    }

    // In the old order; Web2 numbers them 1..n. A package left with one puzzle is no package.
    private async Task ImportPuzzlesAsync(SourceData source, SourceGameDay oldDay, GameDay day, string dayWhat, CancellationToken cancellationToken)
    {
        foreach (SourcePuzzle old in source.Puzzles[oldDay.Id])
        {
            string what = $"Puzzle {old.Id} ({dayWhat}, #{old.Order})";
            int? group = old.Group;
            if (group is not null && (group < 1 || group > Puzzle.GroupMax))
            {
                _report.Changed(what, $"package {group} is out of range; left without a package.");
                group = null;
            }

            Puzzle puzzle = day.AddPuzzle(group, _sanitizer.Sanitize(old.Question), _sanitizer.Sanitize(old.Answer));
            SetId(puzzle, _idMap.For("Puzzles", old.Id));

            string directory = GameDayPaths.PuzzleDirectory(day.GameId, day.Id, puzzle.Id);
            foreach (SourceMedia media in source.PuzzleMedia[old.Id])
            {
                PuzzleMediaKind kind = media.Kind?.Trim().ToUpperInvariant() switch
                {
                    "A" => PuzzleMediaKind.Answer,
                    "Q" => PuzzleMediaKind.Question,
                    _ => PuzzleMediaKind.Question
                };
                if (media.Kind?.Trim().ToUpperInvariant() is not ("A" or "Q"))
                {
                    _report.Changed(what, $"media {media.Id} has kind '{media.Kind}'; imported as a question picture.");
                }

                StoredMedia? stored = await CopyAsync(media.Path, directory, $"{what} media {media.Id}", cancellationToken);
                if (stored is not null)
                {
                    PuzzleMedia added = puzzle.AddMedia(kind, stored.Path, stored.FileName, OriginalName(media), Utc(media.TimeStamp));
                    SetId(added, _idMap.For("PuzzleMedia", media.Id));
                }
            }
        }

        foreach (IGrouping<int?, Puzzle> package in day.Puzzles.Where(p => p.Group is not null).GroupBy(p => p.Group).ToList())
        {
            if (package.Count() == 1)
            {
                Puzzle alone = package.Single();
                day.UpdatePuzzle(alone.Id, null, alone.Question, alone.Answer);
                _report.Changed($"Puzzle #{alone.Order} ({dayWhat})", $"was the only one in package {package.Key}; left without a package.");
            }
        }
    }

    // ---------- Posts ----------

    private async Task ImportPostsAsync(SourceData source, CancellationToken cancellationToken)
    {
        foreach (SourcePost old in source.Posts)
        {
            string what = $"Post {old.Id} (game {old.GameId})";
            if (!_gameIds.TryGetValue(old.GameId, out Guid gameId))
            {
                _report.Skipped(what, "its game wasn't imported.");
                continue;
            }

            Post post;
            try
            {
                post = Post.Create(gameId, Truncate(old.Title, Post.TitleMaxLength, what, "title"), _sanitizer.Sanitize(old.Content), Utc(old.CreatedAt));
            }
            catch (DomainException exception)
            {
                _report.Skipped(what, exception.Message);
                continue;
            }

            SetId(post, _idMap.For("Posts", old.Id));
            _dbContext.Posts.Add(post);

            string directory = PostMediaPaths.Directory(gameId, post.Id);
            foreach (SourceMedia media in source.PostMedia[old.Id])
            {
                StoredMedia? stored = await CopyAsync(media.Path, directory, $"{what} media {media.Id}", cancellationToken);
                if (stored is not null)
                {
                    PostMedia added = post.AddMedia(stored.Path, stored.FileName, OriginalName(media), Utc(media.TimeStamp));
                    SetId(added, _idMap.For("PostMedia", media.Id));
                }
            }
        }
    }

    // ---------- Files ----------

    // An old stored path ("media/games/2024/abc.jpg") through Web2's upload pipeline into the new folder. A dry
    // run only checks that the file is there and allowed.
    private async Task<StoredMedia?> CopyAsync(string? oldPath, string directory, string what, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldPath))
        {
            return null;
        }

        string relative = oldPath.Replace('\\', '/').TrimStart('/');
        if (relative.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
        {
            relative = relative["media/".Length..];
        }

        _referencedFiles.Add(relative);

        string fullPath = Path.GetFullPath(Path.Combine(_settings.SourceMediaRoot, relative));
        if (!fullPath.StartsWith(_settings.SourceMediaRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            _report.Skipped(what, $"path '{oldPath}' points outside the media folder.");
            return null;
        }

        FileInfo file = new FileInfo(fullPath);
        if (!file.Exists)
        {
            _report.Skipped(what, $"file '{oldPath}' is missing.");
            return null;
        }

        try
        {
            MediaRules.Validate(new MediaUpload(Stream.Null, file.Name, file.Length));
            if (!_settings.Apply)
            {
                return new StoredMedia($"media/{directory}/{file.Name}", file.Name);
            }

            await using FileStream stream = file.OpenRead();
            return await _mediaStorage.SaveAsync(directory, new MediaUpload(stream, file.Name, file.Length), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            _report.Skipped(what, $"'{oldPath}': {exception.Message}");
            return null;
        }
    }

    // Files on disk that no row points at (#22): reported, never imported.
    private void ReportUnreferencedFiles()
    {
        foreach (string file in Directory.EnumerateFiles(_settings.SourceMediaRoot, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(_settings.SourceMediaRoot, file).Replace('\\', '/');
            if (!Path.GetFileName(relative).StartsWith(MediaRules.ThumbnailPrefix, StringComparison.Ordinal)
                && !_referencedFiles.Contains(relative))
            {
                _report.Unreferenced(relative);
            }
        }
    }

    // ---------- Helpers ----------

    private Guid? TeamId(long oldTeamId, string what, string role)
    {
        if (_teamIds.TryGetValue(oldTeamId, out Guid id))
        {
            return id;
        }

        _report.Changed(what, $"{role} (old team {oldTeamId}) wasn't imported; left empty.");
        return null;
    }

    private static GameDayType? DayType(string label)
    {
        // By label, or by its slug so accents and case don't matter ("ΣΑΒΒΑΤΟ", "Σαββατο").
        string? slug = TrySlug(label);
        return GameDayTypes.All.Cast<GameDayType?>().FirstOrDefault(type => type!.Value.Slug() == slug);
    }

    private static string? TrySlug(string text)
    {
        try
        {
            return Slug.FromName(text).Value;
        }
        catch (DomainException)
        {
            return null;
        }
    }

    // Only absolute http(s) links are allowed. "www.example.com" gets its https:// back; anything else is dropped.
    private string? Link(string? value, string what, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string link = value.Trim();
        if (!link.Contains("://", StringComparison.Ordinal))
        {
            link = "https://" + link;
        }

        if (link.Length <= Team.UrlMaxLength
            && Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && uri.Host.Contains('.', StringComparison.Ordinal))
        {
            if (link != value.Trim())
            {
                _report.Changed(what, $"{label} link '{value}' became '{link}'.");
            }

            return link;
        }

        _report.Changed(what, $"{label} link '{value}' isn't a valid http(s) address; left out.");
        return null;
    }

    private string? Truncate(string? value, int maxLength, string what, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        _report.Changed(what, $"{field} cut to {maxLength} characters.");
        return trimmed[..maxLength].TrimEnd();
    }

    private static string OriginalName(SourceMedia media)
    {
        string name = Path.GetFileName(media.OriginalFileName.Replace('\\', '/'));
        return name.Length <= MediaUploads.OriginalNameMaxLength ? name : name[..MediaUploads.OriginalNameMaxLength];
    }

    // The old app stored UTC (DateTime.UtcNow and the container's CURRENT_TIMESTAMP).
    private static DateTimeOffset Utc(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    // Domain objects make their own ids; the import gives them the ids from the map instead, so a row keeps its id
    // from run to run. Set before any child is added, since children copy their parent's id.
    private static void SetId(object entity, Guid id)
    {
        PropertyInfo property = entity.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{entity.GetType().Name} has no Id.");
        property.SetValue(entity, id);
    }
}
