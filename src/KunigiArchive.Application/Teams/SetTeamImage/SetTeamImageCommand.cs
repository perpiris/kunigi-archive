using KunigiArchive.Application.Media;

namespace KunigiArchive.Application.Teams.SetTeamImage;

// A null image removes the current one.
public sealed record SetTeamImageCommand(Guid TeamId, MediaUpload? Image);
