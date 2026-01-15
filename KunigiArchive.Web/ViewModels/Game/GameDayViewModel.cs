namespace KunigiArchive.Web.ViewModels.Game;

public class GameDayViewModel
{
    public long GameDayId { get; set; }
    public required string Name { get; set; }
    public bool IsEnabled { get; set; }
}