using KunigiArchive.Web.ViewModels.Common;

namespace KunigiArchive.Web.ViewModels.Game;

public class GameDetailsViewModel
{
    public long GameId { get; set; }
    public required string Slug { get; set; }
    public short Year { get; set; }
    public short Order { get; set; }
    public required string OrderTitle { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public long? WinnerId { get; set; }
    public string? Winner { get; set; }
    public string? WinnerSlug { get; set; }
    public long HostId { get; set; }
    public string Host { get; set; } = string.Empty;
    public string HostSlug { get; set; } = string.Empty;
    public List<GameDayViewModel> GameDays { get; set; } = [];
    public List<MediaFileViewModel> Media { get; set; } = [];
    public List<PostDetailsViewModel> Posts { get; set; } = [];

    public string FormattedTitle()
    {
        string result = $"{OrderTitle} {Year}";

        if (!string.IsNullOrWhiteSpace(Title))
        {
            result += $" - {Title}";
        }

        return result;
    }
}