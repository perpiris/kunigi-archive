using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;

namespace KunigiArchive.Application.Users.GetUsers;

public sealed class GetUsersHandler
{
    // Admin-only and small: one page keeps the in-browser column sort correct (#80).
    private const int PageSize = 500;

    private readonly IUserQueries _userQueries;
    private readonly ICurrentUser _currentUser;

    public GetUsersHandler(IUserQueries userQueries, ICurrentUser currentUser)
    {
        _userQueries = userQueries;
        _currentUser = currentUser;
    }

    public async Task<UsersPage> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        if (search?.Length > GetUsersQuery.SearchMaxLength)
        {
            search = search[..GetUsersQuery.SearchMaxLength];
        }

        return await _userQueries.GetPageAsync(query.After, search, PageSize, cancellationToken);
    }
}
