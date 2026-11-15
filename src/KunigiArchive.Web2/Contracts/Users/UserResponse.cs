namespace KunigiArchive.Web2.Contracts.Users;

public sealed record UserResponse(Guid Id, string Email, string? Role);
