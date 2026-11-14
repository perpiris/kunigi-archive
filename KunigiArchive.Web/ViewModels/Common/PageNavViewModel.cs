namespace KunigiArchive.Web.ViewModels.Common;

public class PageNavViewModel
{
    public required string Controller { get; set; }
    public required string Action { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public string? SearchQuery { get; set; }
}