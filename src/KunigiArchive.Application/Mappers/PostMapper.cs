using KunigiArchive.Application.Media;
using KunigiArchive.Application.Posts;
using KunigiArchive.Domain.Posts;

namespace KunigiArchive.Application.Mappers;

public static class PostMapper
{
    public static PostDetails ToDetails(this Post post)
    {
        return new PostDetails(
            post.Id,
            post.GameId,
            post.Title,
            post.Content,
            post.CreatedAt,
            post.Media.OrderBy(m => m.UploadedAt).Select(ToDetails).ToList());
    }

    public static PostMediaDetails ToDetails(this PostMedia media)
    {
        return new PostMediaDetails(
            media.Id,
            media.Path,
            MediaRules.ThumbnailFor(media.Path),
            media.OriginalFileName,
            media.UploadedAt);
    }
}