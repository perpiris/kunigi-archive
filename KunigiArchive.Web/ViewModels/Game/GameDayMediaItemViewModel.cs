namespace KunigiArchive.Web.ViewModels.Game;

public class GameDayMediaItemViewModel
{
    public long GameDayMediaId { get; set; }
    public required string Path { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileName { get; set; }
    public DateTime TimeStamp { get; set; }
}