using System.ComponentModel.DataAnnotations;

namespace KunigiArchive.Web.ViewModels.Authentication;

public class ResetPasswordViewModel
{
    [Required][EmailAddress] public string Email { get; set; } = null!;

    [Required] public string Token { get; set; } = null!;

    [Required(ErrorMessage = "Ο κωδικός πρόσβασης είναι υποχρεωτικός")]
    [StringLength(100, ErrorMessage = "Ο κωδικός πρέπει να είναι μεταξύ {2} και μέγιστο {1} χαρακτήρες.",
        MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Οι κωδικοί δεν ταιριάζουν.")]
    public string ConfirmPassword { get; set; } = null!;
}