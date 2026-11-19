using System.ComponentModel.DataAnnotations;
using KunigiArchive.Domain.Posts;

namespace KunigiArchive.Web.Contracts.Posts;

// For both creating and editing a post. Content is rich text (HTML); it is sanitized on save.
public sealed record PostRequest
{
    [StringLength(Post.TitleMaxLength)]
    public string? Title { get; init; }

    [Required]
    public required string Content { get; init; }
}