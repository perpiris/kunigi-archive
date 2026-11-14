using KunigiArchive.Application.Media;

namespace KunigiArchive.Application.Teams.UploadTeamMedia;

public sealed record UploadTeamMediaCommand(Guid TeamId, IReadOnlyList<MediaUpload> Files);