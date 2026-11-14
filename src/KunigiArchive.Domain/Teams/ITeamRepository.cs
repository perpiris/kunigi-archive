using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.Teams;

public interface ITeamRepository
{
    Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Team team);
}