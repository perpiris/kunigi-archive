using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.Teams;

public sealed class Team
{
    public const int NameMaxLength = 150;
    public const int UrlMaxLength = 500;
    public const short YearFoundedMin = 1900;

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

    private readonly List<TeamManager> _managers = [];
    public IReadOnlyCollection<TeamManager> Managers => _managers.AsReadOnly();

    private readonly List<TeamMedia> _media = [];
    public IReadOnlyCollection<TeamMedia> Media => _media.AsReadOnly();

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

    public void Update(
        string name,
        bool isActive,
        short? yearFounded,
        string? description,
        string? facebook,
        string? instagram,
        string? youtube,
        string? website)
    {
        string trimmedName = ValidateName(name);

        if (yearFounded is not null && yearFounded < YearFoundedMin)
        {
            throw new DomainException($"Το έτος ίδρυσης δεν μπορεί να είναι πριν το {YearFoundedMin}.");
        }

        Name = trimmedName;
        IsActive = isActive;
        YearFounded = yearFounded;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Facebook = ValidateUrl(facebook, "Facebook");
        Instagram = ValidateUrl(instagram, "Instagram");
        Youtube = ValidateUrl(youtube, "YouTube");
        Website = ValidateUrl(website, "ιστοσελίδα");
    }

    // The slug is the public URL, so it only changes on purpose, never as a side effect of renaming.
    public void ChangeSlug(Slug slug)
    {
        Slug = slug;
    }

    public TeamMedia AddMedia(string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        TeamMedia media = new TeamMedia(Id, path, fileName, originalFileName, uploadedAt);
        _media.Add(media);
        return media;
    }

    public TeamMedia RemoveMedia(Guid mediaId)
    {
        TeamMedia media = _media.FirstOrDefault(m => m.Id == mediaId)
            ?? throw new DomainException("Το αρχείο δεν ανήκει στην ομάδα.");

        _media.Remove(media);
        return media;
    }

    public bool IsManagedBy(Guid userId)
    {
        return _managers.Any(m => m.UserId == userId);
    }

    public void AssignManager(Guid userId)
    {
        if (IsManagedBy(userId))
        {
            throw new DomainException("Ο χρήστης είναι ήδη διαχειριστής της ομάδας.");
        }

        _managers.Add(new TeamManager(Id, userId));
    }

    public void RemoveManager(Guid userId)
    {
        TeamManager manager = _managers.FirstOrDefault(m => m.UserId == userId)
            ?? throw new DomainException("Ο χρήστης δεν είναι διαχειριστής της ομάδας.");

        _managers.Remove(manager);
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

    // Only absolute http(s) links: these are rendered as hrefs, so "javascript:" and the like must never get in.
    private static string? ValidateUrl(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > UrlMaxLength
            || !Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException($"Ο σύνδεσμος για {label} δεν είναι έγκυρος. Χρησιμοποιήστε μια πλήρη διεύθυνση https://.");
        }

        return trimmed;
    }
}