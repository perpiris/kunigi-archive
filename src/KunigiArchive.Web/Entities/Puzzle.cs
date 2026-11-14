namespace KunigiArchive.Web.Entities;

public class Puzzle
{
    public long PuzzleId { get; set; }
    public long GameDayId { get; set; }
    public short Order { get; set; }
    public short? Group { get; set; }
    public string? Question { get; set; }
    public string? Answer { get; set; }

    public virtual GameDay GameDay { get; set; } = null!;
    public virtual ICollection<PuzzleMedia> MediaFiles { get; set; } = [];
}