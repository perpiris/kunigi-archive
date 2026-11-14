namespace KunigiArchive.Application.Teams;

public sealed record TeamMediaDetails(Guid Id, string Path, string OriginalFileName, DateTimeOffset UploadedAt);