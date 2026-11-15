namespace KunigiArchive.Application.Games;

public static class GameMediaPaths
{
    // By id, not year or slug: the folder never has to move when the admin corrects the record.
    public static string Directory(Guid gameId)
    {
        return $"games/{gameId}";
    }
}