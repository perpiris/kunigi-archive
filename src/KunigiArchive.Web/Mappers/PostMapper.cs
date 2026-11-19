using KunigiArchive.Application.Posts;
using KunigiArchive.Web.Contracts.Posts;

namespace KunigiArchive.Web.Mappers;

public static class PostMapper
{
    public static PostResponse ToResponse(this PostDetails post)
    {
        return new PostResponse(
            post.Id,
            post.GameId,
            post.Title,
            post.Content,
            post.CreatedAt,
            post.Media.Select(ToResponse).ToList());
    }

    public static PostMediaResponse ToResponse(this PostMediaDetails media)
    {
        return new PostMediaResponse(media.Id, media.Path, media.ThumbnailPath, media.OriginalFileName, media.UploadedAt);
    }
}