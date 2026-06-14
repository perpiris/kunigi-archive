using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KunigiArchive.Web.ViewModels.Game;

public class GameCreateViewModel
{
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
    public long? WinnerId { get; set; }

    public SelectList HostSelectList { get; set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList WinnerSelectList { get; set; } = new(Enumerable.Empty<SelectListItem>());
}