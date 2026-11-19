using System.ComponentModel.DataAnnotations;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Users;

namespace KunigiArchive.Web.Forms.Users;

public sealed record CreateUserForm
{
    [Required(ErrorMessage = "Το email είναι υποχρεωτικό.")]
    [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο.")]
    [StringLength(UserRules.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    // Most new accounts are team managers.
    public string Role { get; set; } = Roles.Manager;
}
