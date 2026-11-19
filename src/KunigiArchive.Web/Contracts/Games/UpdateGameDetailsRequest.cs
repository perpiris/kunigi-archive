using System.ComponentModel.DataAnnotations;
using KunigiArchive.Domain.Games;

namespace KunigiArchive.Web.Contracts.Games;

// The host team's part of a game. Description is rich text (HTML); it is sanitized on save.
public sealed record UpdateGameDetailsRequest
{
    [StringLength(Game.TitleMaxLength)]
    public string? Title { get; init; }

    [StringLength(Game.NoteMaxLength)]
    public string? Note { get; init; }

    public string? Description { get; init; }
}