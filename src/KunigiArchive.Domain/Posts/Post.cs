using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.Posts;

// News about a game, written by the host team: announcements, results, thanks. Its own aggregate, so
// editing a game never loads every post.
public sealed class Post
{
    public const int TitleMaxLength = 200;

    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string? Title { get; private set; }

    // Rich text (sanitized HTML).
    public string Content { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<PostMedia> _media = [];
    public IReadOnlyCollection<PostMedia> Media => _media.AsReadOnly();

    private Post()
    {
    }

    public static Post Create(Guid gameId, string? title, string? content, DateTimeOffset createdAt)
    {
        Post post = new() { Id = Guid.CreateVersion7(), GameId = gameId, CreatedAt = createdAt };
        post.Update(title, content);
        return post;
    }

    public void Update(string? title, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Το κείμενο της ανάρτησης είναι υποχρεωτικό.");
        }

        string? trimmedTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        if (trimmedTitle?.Length > TitleMaxLength)
        {
            throw new DomainException($"Ο τίτλος δεν μπορεί να ξεπερνά τους {TitleMaxLength} χαρακτήρες.");
        }

        Title = trimmedTitle;
        Content = content.Trim();
    }

    public PostMedia AddMedia(string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        PostMedia media = new PostMedia(Id, path, fileName, originalFileName, uploadedAt);
        _media.Add(media);
        return media;
    }

    public PostMedia RemoveMedia(Guid mediaId)
    {
        PostMedia media = _media.FirstOrDefault(m => m.Id == mediaId)
            ?? throw new DomainException("Το αρχείο δεν ανήκει στην ανάρτηση.");
        _media.Remove(media);
        return media;
    }
}