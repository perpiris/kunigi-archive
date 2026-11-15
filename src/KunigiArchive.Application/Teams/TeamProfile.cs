using KunigiArchive.Application.Games;

namespace KunigiArchive.Application.Teams;

// The public team page: the team, its games, and whether the viewer may manage it.
public sealed record TeamProfile(
    TeamDetails Team,
    IReadOnlyList<GameSummary> HostedGames,
    IReadOnlyList<GameSummary> WonGames,
    bool CanEdit);
