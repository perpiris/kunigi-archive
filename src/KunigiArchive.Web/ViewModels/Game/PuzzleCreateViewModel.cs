namespace KunigiArchive.Web.ViewModels.Game;

public class PuzzleCreateViewModel
{
    public long GameDayId { get; set; }
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public short? Group { get; set; }
    public List<IFormFile> QuestionMediaFiles { get; set; } = [];
    public List<IFormFile> AnswerMediaFiles { get; set; } = [];
}