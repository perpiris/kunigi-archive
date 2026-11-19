using System.ComponentModel.DataAnnotations;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Games.GetGames;

namespace KunigiArchive.Web.Contracts.Games;

public sealed record GetGamesRequest : IValidatableObject
{
    public string? After { get; init; }

    [StringLength(GetGamesQuery.SearchMaxLength)]
    public string? Search { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (After is not null && !GamesCursor.TryDecode(After, out _))
        {
            yield return new ValidationResult("'after' is not a valid cursor.", [nameof(After)]);
        }
    }
}