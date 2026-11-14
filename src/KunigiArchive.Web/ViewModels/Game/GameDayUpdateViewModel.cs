using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace KunigiArchive.Web.ViewModels.Game;

public class GameDayUpdateViewModel
{
    public long GameDayId { get; set; }
    public required string GameSlug { get; set; }
    public required string GameDayName { get; set; }
    public required string GameFormattedTitle { get; set; }

    [DisplayName("Τίτλος")]
    [StringLength(150, ErrorMessage = "Ο τίτλος δεν μπορεί να υπερβαίνει τους 150 χαρακτήρες.")]
    public string? Title { get; set; }

    [DisplayName("Περιγραφή")] public string? Description { get; set; }
}