namespace KunigiArchive.Application.Media;

public sealed record MediaUpload(Stream Content, string FileName, long Length);