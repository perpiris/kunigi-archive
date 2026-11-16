using System.ComponentModel.DataAnnotations;
using KunigiArchive.Domain.Games;

namespace KunigiArchive.Web2.Forms.Games;

/// <summary>The admin's part of a game: year, number, host and winner. Used to create and to correct.</summary>
public sealed record GameRecordForm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Η χρονιά είναι υποχρεωτική.")]
    [Range(Game.YearMin, 2100, ErrorMessage = "Η χρονιά πρέπει να είναι από {1} έως {2}.")]
    public short? Year { get; set; }

    // Text, so "31,5" can be typed with a Greek comma; GameFormMapper turns it into a number.
    [RegularExpression(@"^\s*\d{1,3}([.,]\d)?\s*$", ErrorMessage = "Γράψτε έναν αριθμό, π.χ. 34 ή 31,5.")]
    public string? Order { get; set; }

    // Text, not Guid?: "Χωρίς ομάδα" posts an empty value, which the form binding can't turn into a Guid? (the
    // form was silently invalid and a game without a host couldn't be saved). GameFormMapper parses them.
    public string? HostId { get; set; }

    public string? WinnerId { get; set; }
}