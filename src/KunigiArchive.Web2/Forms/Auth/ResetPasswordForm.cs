using System.ComponentModel.DataAnnotations;
using KunigiArchive.Application.Users;

namespace KunigiArchive.Web2.Forms.Auth;

// Email and Code come from the emailed link and travel as hidden fields.
public sealed record ResetPasswordForm
{
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ο νέος κωδικός είναι υποχρεωτικός.")]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.Message)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Οι κωδικοί δεν ταιριάζουν.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
