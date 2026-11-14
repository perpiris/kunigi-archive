namespace KunigiArchive.Web.Entities;

public class GameDayType
{
    public long GameDayTypeId { get; set; }
    public required string Label { get; set; }
    public required string Slug { get; set; }
}