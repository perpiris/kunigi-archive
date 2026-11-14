using System.ComponentModel.DataAnnotations;
using KunigiArchive.Application.Teams;

namespace KunigiArchive.Web2.Contracts.Teams;

public sealed record GetTeamsRequest : IValidatableObject
{
    public string? After { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (After is not null && !TeamsCursor.TryDecode(After, out _))
        {
            yield return new ValidationResult("'after' is not a valid cursor.", [nameof(After)]);
        }
    }
}