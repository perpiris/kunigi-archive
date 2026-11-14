using KunigiArchive.Web.ViewModels.Common;

namespace KunigiArchive.Web.ViewModels.Game;

public class PuzzleEditViewModel
{
    public long PuzzleId { get; set; }
    public long GameDayId { get; set; }
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public short? Group { get; set; }
    public List<IFormFile> QuestionMediaFiles { get; set; } = [];
    public List<IFormFile> AnswerMediaFiles { get; set; } = [];
    public List<MediaFileViewModel> QuestionMedia { get; set; } = [];
    public List<MediaFileViewModel> AnswerMedia { get; set; } = [];
}