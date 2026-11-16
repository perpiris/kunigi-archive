using Microsoft.Data.SqlClient;

namespace KunigiArchive.Import;

// The old app's rows, as read from its database (long ids).
internal sealed record SourceUser(
    long Id,
    string? UserName,
    string? NormalizedUserName,
    string? Email,
    string? NormalizedEmail,
    bool EmailConfirmed,
    string? PasswordHash,
    string? SecurityStamp,
    string? PhoneNumber,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd,
    IReadOnlyList<string> Roles);

internal sealed record SourceTeam(
    long Id, string Name, string Slug, bool IsActive, short? YearFounded, string? Description,
    string? Facebook, string? Instagram, string? Youtube, string? Website, string? Image);

internal sealed record SourceGame(
    long Id, short Year, short Order, string Slug, string? Title, string? Description, string? Image, long HostId, long? WinnerId);

internal sealed record SourceGameDay(
    long Id, long GameId, string TypeLabel, string? Title, string? Description, string? Image, bool IsEnabled);

internal sealed record SourcePuzzle(long Id, long GameDayId, short Order, short? Group, string? Question, string? Answer);

internal sealed record SourcePost(long Id, long GameId, string? Title, string Content, DateTime CreatedAt);

// A media row of any table. Kind is the puzzle's "Q" / "A", null elsewhere.
internal sealed record SourceMedia(long Id, long OwnerId, string Path, string OriginalFileName, DateTime TimeStamp, string? Kind);

internal sealed record SourceData(
    IReadOnlyList<SourceUser> Users,
    IReadOnlyList<SourceTeam> Teams,
    ILookup<long, long> TeamManagers,
    ILookup<long, SourceMedia> TeamMedia,
    IReadOnlyList<SourceGame> Games,
    ILookup<long, SourceMedia> GameMedia,
    IReadOnlyList<SourceGameDay> GameDays,
    ILookup<long, SourceMedia> GameDayMedia,
    ILookup<long, SourcePuzzle> Puzzles,
    ILookup<long, SourceMedia> PuzzleMedia,
    IReadOnlyList<SourcePost> Posts,
    ILookup<long, SourceMedia> PostMedia);

// Reads everything with plain SELECTs: the old database is never written to.
internal static class SourceReader
{
    public static async Task<SourceData> ReadAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        List<(long UserId, string Role)> userRoles = await ReadAsync(connection,
            "SELECT ur.UserId, r.Name FROM AspNetUserRoles ur JOIN AspNetRoles r ON r.Id = ur.RoleId",
            r => (r.GetInt64(0), r.GetString(1)), cancellationToken);
        ILookup<long, string> rolesByUser = userRoles.ToLookup(x => x.UserId, x => x.Role);

        List<SourceUser> users = await ReadAsync(connection,
            """
            SELECT Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash,
                   SecurityStamp, PhoneNumber, LockoutEnabled, LockoutEnd
            FROM AspNetUsers ORDER BY Id
            """,
            r => new SourceUser(
                r.GetInt64(0), Str(r, 1), Str(r, 2), Str(r, 3), Str(r, 4), r.GetBoolean(5), Str(r, 6), Str(r, 7),
                Str(r, 8), r.GetBoolean(9), r.IsDBNull(10) ? null : r.GetFieldValue<DateTimeOffset>(10), []),
            cancellationToken);
        users = users.Select(u => u with { Roles = rolesByUser[u.Id].ToList() }).ToList();

        List<SourceTeam> teams = await ReadAsync(connection,
            "SELECT TeamId, Name, Slug, IsActive, YearFounded, Description, Facebook, Instagram, Youtube, Website, Image FROM Teams ORDER BY TeamId",
            r => new SourceTeam(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetBoolean(3), r.IsDBNull(4) ? null : r.GetInt16(4),
                Str(r, 5), Str(r, 6), Str(r, 7), Str(r, 8), Str(r, 9), Str(r, 10)),
            cancellationToken);

        List<(long TeamId, long UserId)> managers = await ReadAsync(connection,
            "SELECT TeamId, AppUserId FROM TeamManagers", r => (r.GetInt64(0), r.GetInt64(1)), cancellationToken);

        List<SourceGame> games = await ReadAsync(connection,
            "SELECT GameId, [Year], [Order], Slug, Title, Description, Image, HostId, WinnerId FROM Games ORDER BY [Year], GameId",
            r => new SourceGame(r.GetInt64(0), r.GetInt16(1), r.GetInt16(2), r.GetString(3), Str(r, 4), Str(r, 5), Str(r, 6),
                r.GetInt64(7), r.IsDBNull(8) ? null : r.GetInt64(8)),
            cancellationToken);

        List<SourceGameDay> days = await ReadAsync(connection,
            """
            SELECT d.GameDayId, d.GameId, t.Label, d.Title, d.Description, d.Image, d.IsEnabled
            FROM GameDays d JOIN GameDayTypes t ON t.GameDayTypeId = d.GameTypeId ORDER BY d.GameDayId
            """,
            r => new SourceGameDay(r.GetInt64(0), r.GetInt64(1), r.GetString(2), Str(r, 3), Str(r, 4), Str(r, 5), r.GetBoolean(6)),
            cancellationToken);

        List<SourcePuzzle> puzzles = await ReadAsync(connection,
            "SELECT PuzzleId, GameDayId, [Order], [Group], Question, Answer FROM Puzzles ORDER BY GameDayId, [Order], PuzzleId",
            r => new SourcePuzzle(r.GetInt64(0), r.GetInt64(1), r.GetInt16(2), r.IsDBNull(3) ? null : r.GetInt16(3), Str(r, 4), Str(r, 5)),
            cancellationToken);

        List<SourcePost> posts = await ReadAsync(connection,
            "SELECT PostId, GameId, Title, Content, CreatedAt FROM Posts ORDER BY CreatedAt, PostId",
            r => new SourcePost(r.GetInt64(0), r.GetInt64(1), Str(r, 2), r.GetString(3), r.GetDateTime(4)),
            cancellationToken);

        return new SourceData(
            users,
            teams,
            managers.ToLookup(m => m.TeamId, m => m.UserId),
            (await ReadMediaAsync(connection, "TeamMedia", "TeamMediaId", "TeamId", null, cancellationToken)).ToLookup(m => m.OwnerId),
            games,
            (await ReadMediaAsync(connection, "GameMedia", "GameMediaId", "GameId", null, cancellationToken)).ToLookup(m => m.OwnerId),
            days,
            (await ReadMediaAsync(connection, "GameDayMedia", "GameDayMediaId", "GameDayId", null, cancellationToken)).ToLookup(m => m.OwnerId),
            puzzles.ToLookup(p => p.GameDayId),
            (await ReadMediaAsync(connection, "PuzzleMedia", "PuzzleMediaId", "PuzzleId", "MediaType", cancellationToken)).ToLookup(m => m.OwnerId),
            posts,
            (await ReadMediaAsync(connection, "PostMedia", "PostMediaId", "PostId", null, cancellationToken)).ToLookup(m => m.OwnerId));
    }

    // Table and column names are constants of this file, never input.
    private static Task<List<SourceMedia>> ReadMediaAsync(
        SqlConnection connection, string table, string idColumn, string ownerColumn, string? kindColumn, CancellationToken cancellationToken)
    {
        string kind = kindColumn is null ? "CAST(NULL AS nvarchar(50))" : $"[{kindColumn}]";
        return ReadAsync(connection,
            $"SELECT [{idColumn}], [{ownerColumn}], Path, OriginalFileName, TimeStamp, {kind} FROM [{table}] ORDER BY TimeStamp, [{idColumn}]",
            r => new SourceMedia(r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetDateTime(4), Str(r, 5)),
            cancellationToken);
    }

    private static async Task<List<T>> ReadAsync<T>(
        SqlConnection connection, string sql, Func<SqlDataReader, T> map, CancellationToken cancellationToken)
    {
        await using SqlCommand command = new SqlCommand(sql, connection);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        List<T> rows = [];
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(map(reader));
        }

        return rows;
    }

    private static string? Str(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
