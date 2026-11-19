using System.ComponentModel.DataAnnotations;
using KunigiArchive.Application.Users;
using KunigiArchive.Application.Users.GetUsers;

namespace KunigiArchive.Web.Contracts.Users;

public sealed record GetUsersRequest : IValidatableObject
{
    public string? After { get; init; }

    [StringLength(GetUsersQuery.SearchMaxLength)]
    public string? Search { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (After is not null && !UsersCursor.TryDecode(After, out _))
        {
            yield return new ValidationResult("'after' is not a valid cursor.", [nameof(After)]);
        }
    }
}
