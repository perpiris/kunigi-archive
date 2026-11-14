namespace KunigiArchive.Application.Media;

// Path is web-relative ("media/teams/{id}/{file}"), which is what the entities store.
public sealed record StoredMedia(string Path, string FileName);