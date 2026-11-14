using System.ComponentModel.DataAnnotations;

namespace KunigiArchive.Web.ViewModels.Authentication;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Το email είναι υποχρεωτικό")]
    [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο")]
    public string Email { get; set; } = null!;
}