namespace KunigiArchive.Web.ViewModels.Team;

public class TeamMediaItemViewModel
{
    public long TeamMediaId { get; set; }
    public long TeamId { get; set; }
    public required string Path { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileName { get; set; }
    public DateTime TimeStamp { get; set; }
}