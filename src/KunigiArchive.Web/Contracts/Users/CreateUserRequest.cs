namespace KunigiArchive.Web.Contracts.Users;

// Role: "Admin", "Manager", or null for no role.
public sealed record CreateUserRequest
{
    public required string Email { get; init; }
    public string? Role { get; init; }
}
