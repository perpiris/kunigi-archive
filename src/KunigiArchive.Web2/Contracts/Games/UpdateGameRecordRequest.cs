namespace KunigiArchive.Web2.Contracts.Games;

public sealed record UpdateGameRecordRequest
{
    public required short Year { get; init; }
    public decimal? Order { get; init; }
    public Guid? HostId { get; init; }
    public Guid? WinnerId { get; init; }
}