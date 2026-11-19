namespace KunigiArchive.Web.Contracts.Users;

public sealed record UserResponse(Guid Id, string Email, string? Role);
