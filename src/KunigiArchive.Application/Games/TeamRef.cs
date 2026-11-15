namespace KunigiArchive.Application.Games;

/// <summary>A team as a game shows it: enough to print the name and link to the team page.</summary>
public sealed record TeamRef(Guid Id, string Name, string Slug);