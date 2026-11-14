using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.Teams;

public sealed class Team
{
    public const int NameMaxLength = 150;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public short? YearFounded { get; private set; }
    public string? Description { get; private set; }
    public string? Facebook { get; private set; }
    public string? Instagram { get; private set; }
    public string? Youtube { get; private set; }
    public string? Website { get; private set; }
    public string? Image { get; private set; }

    private Team()
    {
    }

    public static Team Create(string name, bool isActive)
    {
        string trimmedName = ValidateName(name);

        return new Team
        {
            Id = Guid.CreateVersion7(),
            Name = trimmedName,
            Slug = Slug.FromName(trimmedName),
            IsActive = isActive
        };
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Το όνομα της ομάδας είναι υποχρεωτικό.");
        }

        string trimmedName = name.Trim();
        if (trimmedName.Length > NameMaxLength)
        {
            throw new DomainException($"Το όνομα της ομάδας δεν μπορεί να ξεπερνά τους {NameMaxLength} χαρακτήρες.");
        }

        return trimmedName;
    }
}