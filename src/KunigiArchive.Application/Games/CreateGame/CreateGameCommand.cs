namespace KunigiArchive.Application.Games.CreateGame;

public sealed record CreateGameCommand(short Year, decimal? Order, Guid? HostId, Guid? WinnerId);