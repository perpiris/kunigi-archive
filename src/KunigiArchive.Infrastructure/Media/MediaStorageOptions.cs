namespace KunigiArchive.Infrastructure.Media;

public sealed record MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    // Relative paths resolve against the app's content root.
    public string RootPath { get; set; } = string.Empty;
}