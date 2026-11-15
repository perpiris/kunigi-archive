using KunigiArchive.Application.Games;
using KunigiArchive.Domain.Common;

namespace KunigiArchive.Application.Teams;

public interface ITeamQueries
{
    Task<TeamDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TeamDetails?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeamSummary>> GetManagedByAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> IsManagedByAsync(Guid teamId, Guid userId, CancellationToken cancellationToken);
    Task<TeamsPage> GetPageAsync(TeamsCursor? after, string? search, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<GameSummary>> GetHostedGamesAsync(Guid teamId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GameSummary>> GetWonGamesAsync(Guid teamId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeamOption>> GetOptionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TeamRef>> GetRefsAsync(CancellationToken cancellationToken);
}