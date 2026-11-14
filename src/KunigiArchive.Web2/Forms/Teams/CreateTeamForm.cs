using System.ComponentModel.DataAnnotations;
using KunigiArchive.Domain.Teams;

namespace KunigiArchive.Web2.Forms.Teams;

public sealed record CreateTeamForm
{
    [Required(ErrorMessage = "Το όνομα της ομάδας είναι υποχρεωτικό.")]
    [StringLength(Team.NameMaxLength, ErrorMessage = "Το όνομα δεν μπορεί να ξεπερνά τους {1} χαρακτήρες.")]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}