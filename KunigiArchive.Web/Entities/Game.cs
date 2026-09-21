namespace KunigiArchive.Web.Entities;

public class Game
{
    public long GameId { get; set; }
    public short Year { get; set; }
    public short Order { get; set; }
    public required string Slug { get; set; }
    public required string OrderTitle { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public long HostId { get; set; }
    public long? WinnerId { get; set; }

    public virtual Team Host { get; set; } = null!;
    public virtual Team? Winner { get; set; }
    public virtual ICollection<GameDay> GameDays { get; set; } = [];
    public virtual ICollection<GameMedia> MediaFiles { get; set; } = [];
    public virtual ICollection<Post> Posts { get; set; } = [];
}