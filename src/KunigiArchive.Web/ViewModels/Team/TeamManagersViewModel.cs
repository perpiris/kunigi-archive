using System.ComponentModel.DataAnnotations;
using KunigiArchive.Web.ViewModels.Account;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KunigiArchive.Web.ViewModels.Team;

public class TeamManagersViewModel
{
    public long TeamId { get; set; }

    public required string TeamName { get; set; }

    public required string Slug { get; set; }

    [Required(ErrorMessage = "Το πεδίο απαιτείται.")]
    public long SelectedUserId { get; set; }

    public List<AccountDetailsViewModel> CurrentManagers { get; set; } = new();

    public SelectList? AvailableUsers { get; set; }
}