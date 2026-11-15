namespace KunigiArchive.Application.Teams;

/// <summary>A team in a picker: every team, active or not, by name.</summary>
public sealed record TeamOption(Guid Id, string Name, bool IsActive);