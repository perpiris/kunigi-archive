namespace KunigiArchive.Application.Users.GetUsers;

// Search filters by email; the cursor only makes sense together with the same search.
public sealed record GetUsersQuery(UsersCursor? After, string? Search)
{
    public const int SearchMaxLength = 100;
}
