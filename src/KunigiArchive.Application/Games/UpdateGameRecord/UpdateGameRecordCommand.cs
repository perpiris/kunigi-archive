namespace KunigiArchive.Application.Games.UpdateGameRecord;

public sealed record UpdateGameRecordCommand(Guid Id, short Year, decimal? Order, Guid? HostId, Guid? WinnerId);