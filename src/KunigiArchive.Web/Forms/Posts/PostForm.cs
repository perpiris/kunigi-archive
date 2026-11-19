using System.ComponentModel.DataAnnotations;
using KunigiArchive.Domain.Posts;

namespace KunigiArchive.Web.Forms.Posts;

public sealed record PostForm
{
    // Empty for a new post.
    public Guid Id { get; set; }

    [StringLength(Post.TitleMaxLength, ErrorMessage = "Ο τίτλος δεν μπορεί να ξεπερνά τους {1} χαρακτήρες.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Το κείμενο της ανάρτησης είναι υποχρεωτικό.")]
    public string? Content { get; set; }
}