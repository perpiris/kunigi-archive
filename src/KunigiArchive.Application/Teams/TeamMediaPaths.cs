namespace KunigiArchive.Application.Teams;

public static class TeamMediaPaths
{
    // By id, not slug: the folder never has to move when an admin changes the slug.
    public static string Directory(Guid teamId)
    {
        return $"teams/{teamId}";
    }
}
