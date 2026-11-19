using System.Globalization;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Games.CreateGame;
using KunigiArchive.Application.Games.UpdateGameDetails;
using KunigiArchive.Application.Games.UpdateGameRecord;
using KunigiArchive.Web.Forms.Games;

namespace KunigiArchive.Web.Mappers;

public static class GameFormMapper
{
    private static readonly CultureInfo Greek = CultureInfo.GetCultureInfo("el-GR");

    public static CreateGameCommand ToCreateCommand(this GameRecordForm form)
    {
        return new CreateGameCommand(form.Year!.Value, ParseOrder(form.Order), ParseTeam(form.HostId), ParseTeam(form.WinnerId));
    }

    public static UpdateGameRecordCommand ToUpdateCommand(this GameRecordForm form)
    {
        return new UpdateGameRecordCommand(
            form.Id, form.Year!.Value, ParseOrder(form.Order), ParseTeam(form.HostId), ParseTeam(form.WinnerId));
    }

    // Empty is "no team". Anything that isn't an id is treated the same; the handler checks the team exists.
    private static Guid? ParseTeam(string? id)
    {
        return Guid.TryParse(id, out Guid teamId) ? teamId : null;
    }

    public static GameDetailsForm ToDetailsForm(this GameDetails game)
    {
        return new GameDetailsForm { Id = game.Id, Title = game.Title, Note = game.Note, Description = game.Description };
    }

    public static UpdateGameDetailsCommand ToCommand(this GameDetailsForm form)
    {
        return new UpdateGameDetailsCommand(form.Id, form.Title, form.Note, form.Description);
    }

    public static GameRecordForm ToForm(this GameRecord game)
    {
        return new GameRecordForm
        {
            Id = game.Id,
            Year = game.Year,
            Order = game.Order?.ToString("0.#", Greek),
            HostId = game.HostId?.ToString(),
            WinnerId = game.WinnerId?.ToString()
        };
    }

    // The form already checked the shape (digits, optional ",5" or ".5").
    private static decimal? ParseOrder(string? order)
    {
        return string.IsNullOrWhiteSpace(order)
            ? null
            : decimal.Parse(order.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    }
}