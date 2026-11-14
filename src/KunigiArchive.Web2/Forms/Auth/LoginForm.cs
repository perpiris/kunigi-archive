using System.ComponentModel.DataAnnotations;

namespace KunigiArchive.Web2.Forms.Auth;

public sealed record LoginForm
{
    [Required(ErrorMessage = "Το email είναι υποχρεωτικό.")]
    [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ο κωδικός είναι υποχρεωτικός.")]
    public string Password { get; set; } = string.Empty;
}