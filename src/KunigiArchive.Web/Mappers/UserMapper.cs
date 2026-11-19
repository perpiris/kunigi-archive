using KunigiArchive.Application.Users;
using KunigiArchive.Application.Users.CreateUser;
using KunigiArchive.Application.Users.GetUsers;
using KunigiArchive.Web.Contracts.Users;

namespace KunigiArchive.Web.Mappers;

public static class UserMapper
{
    public static CreateUserCommand ToCommand(this CreateUserRequest request)
    {
        return new CreateUserCommand(request.Email, request.Role);
    }

    public static GetUsersQuery ToQuery(this GetUsersRequest request)
    {
        UsersCursor.TryDecode(request.After, out UsersCursor? after);
        return new GetUsersQuery(after, request.Search);
    }

    public static UserResponse ToResponse(this UserAccount user)
    {
        return new UserResponse(user.Id, user.Email, user.Role);
    }

    public static UsersPageResponse ToResponse(this UsersPage page)
    {
        return new UsersPageResponse(page.Items.Select(ToResponse).ToList(), page.NextCursor);
    }

    public static UserDetailsResponse ToResponse(this UserDetails user)
    {
        return new UserDetailsResponse(
            user.Id,
            user.Email,
            user.Role,
            user.ManagedTeams.Select(team => team.ToResponse()).ToList(),
            user.IsCurrentUser);
    }

    public static CreateUserResponse ToResponse(this CreateUserResult result)
    {
        return new CreateUserResponse(result.UserId);
    }
}
