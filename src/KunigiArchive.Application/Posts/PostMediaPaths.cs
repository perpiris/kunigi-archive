using KunigiArchive.Application.Games;

namespace KunigiArchive.Application.Posts;

public static class PostMediaPaths
{
    // Inside the game's folder, so deleting the game takes its posts' files too.
    public static string Directory(Guid gameId, Guid postId)
    {
        return $"{GameMediaPaths.Directory(gameId)}/posts/{postId}";
    }
}