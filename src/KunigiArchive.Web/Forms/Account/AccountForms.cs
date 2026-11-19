using System.ComponentModel.DataAnnotations;
using KunigiArchive.Application.Users;

namespace KunigiArchive.Web.Forms.Account;

public sealed record ChangeEmailForm
{
    [Required(ErrorMessage = "Το νέο email είναι υποχρεωτικό.")]
    [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο.")]
    [StringLength(UserRules.EmailMaxLength)]
    public string NewEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Γράψτε τον τωρινό κωδικό σας.")]
    public string CurrentPassword { get; set; } = string.Empty;
}

public sealed record ChangePasswordForm
{
    [Required(ErrorMessage = "Γράψτε τον τωρινό κωδικό σας.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ο νέος κωδικός είναι υποχρεωτικός.")]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.Message)]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "Οι κωδικοί δεν ταιριάζουν.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}