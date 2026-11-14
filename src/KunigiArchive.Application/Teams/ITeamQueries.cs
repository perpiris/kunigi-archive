using KunigiArchive.Domain.Common;

namespace KunigiArchive.Application.Teams;

public interface ITeamQueries
{
    Task<TeamDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TeamDetails?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken);
}