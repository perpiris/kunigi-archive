using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace KunigiArchive.Web.ViewModels.Game;

public class PostEditViewModel
{
    public long PostId { get; set; }
    public long GameId { get; set; }

    [DisplayName("Περιεχόμενο")]
    [Required(ErrorMessage = "Το πεδίο απαιτείται.")]
    public string Content { get; set; } = string.Empty;
}