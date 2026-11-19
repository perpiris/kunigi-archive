using System.ComponentModel.DataAnnotations;
using KunigiArchive.Domain.Games;

namespace KunigiArchive.Web.Forms.Games;

// The host team's part of a game.
public sealed record GameDetailsForm
{
    public Guid Id { get; set; }

    [StringLength(Game.TitleMaxLength, ErrorMessage = "Ο τίτλος δεν μπορεί να ξεπερνά τους {1} χαρακτήρες.")]
    public string? Title { get; set; }

    [StringLength(Game.NoteMaxLength, ErrorMessage = "Η σημείωση δεν μπορεί να ξεπερνά τους {1} χαρακτήρες.")]
    public string? Note { get; set; }

    public string? Description { get; set; }
}