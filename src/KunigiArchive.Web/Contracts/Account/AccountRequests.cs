using System.ComponentModel.DataAnnotations;
using KunigiArchive.Application.Users;

namespace KunigiArchive.Web.Contracts.Account;

public sealed record ChangeEmailRequest
{
    [Required]
    [EmailAddress]
    [StringLength(UserRules.EmailMaxLength)]
    public required string NewEmail { get; init; }

    [Required]
    public required string CurrentPassword { get; init; }
}

public sealed record ChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; init; }

    [Required]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength)]
    public required string NewPassword { get; init; }
}