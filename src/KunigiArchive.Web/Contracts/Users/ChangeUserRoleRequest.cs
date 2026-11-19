namespace KunigiArchive.Web.Contracts.Users;

// Role: "Admin", "Manager", or null to remove the role.
public sealed record ChangeUserRoleRequest
{
    public string? Role { get; init; }
}
