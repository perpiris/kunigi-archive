using System.Globalization;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Games.CreateGame;
using KunigiArchive.Application.Games.UpdateGameRecord;
using KunigiArchive.Web2.Forms.Games;

namespace KunigiArchive.Web2.Mappers;

public static class GameFormMapper
{
    private static readonly CultureInfo Greek = CultureInfo.GetCultureInfo("el-GR");

    public static CreateGameCommand ToCreateCommand(this GameRecordForm form)
    {
        return new CreateGameCommand(form.Year!.Value, ParseOrder(form.Order), form.HostId, form.WinnerId);
    }

    public static UpdateGameRecordCommand ToUpdateCommand(this GameRecordForm form)
    {
        return new UpdateGameRecordCommand(form.Id, form.Year!.Value, ParseOrder(form.Order), form.HostId, form.WinnerId);
    }

    public static GameRecordForm ToForm(this GameRecord game)
    {
        return new GameRecordForm
        {
            Id = game.Id,
            Year = game.Year,
            Order = game.Order?.ToString("0.#", Greek),
            HostId = game.HostId,
            WinnerId = game.WinnerId
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