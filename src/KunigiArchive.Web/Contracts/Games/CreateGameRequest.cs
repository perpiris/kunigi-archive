namespace KunigiArchive.Web.Contracts.Games;

// The admin's part of a game. Title, note and the rest belong to the host team's managers.
public sealed record CreateGameRequest
{
    public required short Year { get; init; }

    // Optional; one decimal at most (31.5).
    public decimal? Order { get; init; }

    public Guid? HostId { get; init; }
    public Guid? WinnerId { get; init; }
}