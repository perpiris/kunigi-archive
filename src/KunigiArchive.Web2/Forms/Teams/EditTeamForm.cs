using System.ComponentModel.DataAnnotations;
using KunigiArchive.Domain.Teams;

namespace KunigiArchive.Web2.Forms.Teams;

public sealed record EditTeamForm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Το όνομα της ομάδας είναι υποχρεωτικό.")]
    [StringLength(Team.NameMaxLength, ErrorMessage = "Το όνομα δεν μπορεί να ξεπερνά τους {1} χαρακτήρες.")]
    public string Name { get; set; } = string.Empty;

    // Only rendered for admins; when the field isn't posted it stays null and the slug is left alone.
    [StringLength(KunigiArchive.Domain.Common.Slug.MaxLength)]
    public string? Slug { get; set; }

    public bool IsActive { get; set; }

    [Range(Team.YearFoundedMin, short.MaxValue, ErrorMessage = "Το έτος ίδρυσης δεν μπορεί να είναι πριν το {1}.")]
    public short? YearFounded { get; set; }

    public string? Description { get; set; }

    [StringLength(Team.UrlMaxLength)]
    public string? Facebook { get; set; }

    [StringLength(Team.UrlMaxLength)]
    public string? Instagram { get; set; }

    [StringLength(Team.UrlMaxLength)]
    public string? Youtube { get; set; }

    [StringLength(Team.UrlMaxLength)]
    public string? Website { get; set; }
}