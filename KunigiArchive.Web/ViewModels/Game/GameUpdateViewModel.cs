using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KunigiArchive.Web.ViewModels.Game;

public class GameUpdateViewModel
{
    public long GameId { get; set; }
    public required string Slug { get; set; }

    [DisplayName("Slug")] public string? UpdatedSlug { get; set; }

    [DisplayName("Έτος διεξαγωγής")]
    [Required(ErrorMessage = "Το πεδίο απαιτείται.")]
    public short Year { get; set; }

    [DisplayName("Σειρά")]
    [Required(ErrorMessage = "Το πεδίο απαιτείται.")]
    public short Order { get; set; }

    [DisplayName("Διοργανωτής")]
    [Required(ErrorMessage = "Το πεδίο απαιτείται.")]
    public long HostId { get; set; }

    [DisplayName("Νικητής")]
    [Required(ErrorMessage = "Το πεδίο απαιτείται.")]
    public long WinnerId { get; set; }

    [DisplayName("Περιγραφή")] public string? Description { get; set; }

    [DisplayName("Φωτογραφία")] public string? Image { get; set; }

    public SelectList HostSelectList { get; set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList WinnerSelectList { get; set; } = new(Enumerable.Empty<SelectListItem>());
}