using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace KunigiArchive.Web.ViewModels.Team;

public class TeamUpdateViewModel
{
    public required long TeamId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }

    [DisplayName("Slug")] public required string UpdatedSlug { get; set; }

    [DisplayName("Όνομα")] public required string UpdatedName { get; set; }

    [DisplayName("Ενεργή")] public bool IsActive { get; set; }

    [DisplayName("Έτος Ίδρυσης")] public short? YearFounded { get; set; }

    [DisplayName("Περιγραφή")] public string? Description { get; set; }

    [DisplayName("Facebook")]
    [StringLength(500)]
    public string? Facebook { get; set; }

    [DisplayName("Instagram")]
    [StringLength(500)]
    public string? Instagram { get; set; }

    [DisplayName("Youtube")]
    [StringLength(500)]
    public string? Youtube { get; set; }

    [DisplayName("Website")]
    [StringLength(500)]
    public string? Website { get; set; }

    [DisplayName("Φωτογραφία")] public string? Image { get; set; }
}