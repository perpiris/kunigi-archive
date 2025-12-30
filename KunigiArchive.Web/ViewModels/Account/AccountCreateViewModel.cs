using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KunigiArchive.Web.ViewModels.Account;

public class AccountCreateViewModel
{
    [Required(ErrorMessage = "Το πεδίο απαιτείται.")]
    [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Παρακαλώ επιλέξτε έναν ρόλο.")]
    [Display(Name = "Ρόλος")]
    public string SelectedRole { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> RolesList { get; set; } = new List<SelectListItem>();
}