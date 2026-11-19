namespace KunigiArchive.Web.Contracts.Teams;

public sealed record UpdateTeamRequest
{
    public required string Name { get; init; }
    public string? Slug { get; init; }
    public required bool IsActive { get; init; }
    public short? YearFounded { get; init; }
    public string? Description { get; init; }
    public string? Facebook { get; init; }
    public string? Instagram { get; init; }
    public string? Youtube { get; init; }
    public string? Website { get; init; }
}