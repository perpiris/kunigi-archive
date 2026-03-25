namespace KunigiArchive.Web.Entities;

public class GameDay
{
    public long GameDayId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public bool IsEnabled { get; set; }
    public long GameTypeId { get; set; }
    public long GameId { get; set; }

    public virtual GameDayType GameDayType { get; set; } = null!;
    public virtual Game Game { get; set; } = null!;
    public virtual ICollection<Puzzle> Puzzles { get; set; } = [];
    public virtual ICollection<GameDayMedia> GameDayMedia { get; set; } = [];
}
