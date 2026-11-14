namespace KunigiArchive.Web.ViewModels.Admin;

public class MediaStatusViewModel
{
    public long TotalSizeBytes { get; set; }
    public int TotalFileCount { get; set; }
    public List<DirectoryStatViewModel> Directories { get; set; } = [];
}

public class DirectoryStatViewModel
{
    public required string Name { get; set; }
    public required string RelativePath { get; set; }
    public int FileCount { get; set; }
    public long SizeBytes { get; set; }
    public int SubdirectoryCount { get; set; }
}