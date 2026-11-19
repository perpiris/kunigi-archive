using System.ComponentModel.DataAnnotations;
using KunigiArchive.Domain.GameDays;

namespace KunigiArchive.Web.Forms.GameDays;

public sealed record GameDayForm
{
    public Guid Id { get; set; }

    [StringLength(GameDay.TitleMaxLength, ErrorMessage = "Ο τίτλος δεν μπορεί να ξεπερνά τους {1} χαρακτήρες.")]
    public string? Title { get; set; }

    public string? Description { get; set; }
}

public sealed record PuzzleForm
{
    // Empty for a new puzzle.
    public Guid Id { get; set; }

    [Range(1, Puzzle.GroupMax, ErrorMessage = "Ο αριθμός πακέτου πρέπει να είναι από {1} έως {2}.")]
    public int? Group { get; set; }

    public string? Question { get; set; }

    public string? Answer { get; set; }
}
