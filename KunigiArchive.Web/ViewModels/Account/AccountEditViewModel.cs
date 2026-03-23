using System.ComponentModel.DataAnnotations;
using KunigiArchive.Web.ViewModels.Team;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KunigiArchive.Web.ViewModels.Account;

public class AccountEditViewModel
{
    public long AppUserId { get; set; }

    public required string Email { get; set; }

    [Display(Name = "Ρόλος")] public string SelectedRole { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> RolesList { get; set; } = new List<SelectListItem>();

    public List<TeamDetailsViewModel> ManagedTeams { get; set; } = [];
}