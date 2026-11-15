using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.Teams;

public interface ITeamRepository
{
    Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, Guid excludingTeamId, CancellationToken cancellationToken);
    Task<Team?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Team>> GetManagedByAsync(Guid userId, CancellationToken cancellationToken);
    void Add(Team team);
    void Remove(Team team);
}