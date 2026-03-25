namespace KunigiArchive.Web.ViewModels.Game;

public class GameMediaItemViewModel
{
    public long GameMediaId { get; set; }
    public required string Path { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileName { get; set; }
    public DateTime TimeStamp { get; set; }
}