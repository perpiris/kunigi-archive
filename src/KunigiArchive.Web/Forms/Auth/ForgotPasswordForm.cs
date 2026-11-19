using System.ComponentModel.DataAnnotations;

namespace KunigiArchive.Web.Forms.Auth;

public sealed record ForgotPasswordForm
{
    [Required(ErrorMessage = "Το email είναι υποχρεωτικό.")]
    [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο.")]
    public string Email { get; set; } = string.Empty;
}
