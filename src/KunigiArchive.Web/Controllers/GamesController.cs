using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Games.CreateGame;
using KunigiArchive.Application.Games.DeleteGame;
using KunigiArchive.Application.Games.DeleteGameMedia;
using KunigiArchive.Application.Games.GetGameBySlug;
using KunigiArchive.Application.Games.GetGames;
using KunigiArchive.Application.Games.SetGameImage;
using KunigiArchive.Application.Games.UpdateGameDetails;
using KunigiArchive.Application.Games.UpdateGameRecord;
using KunigiArchive.Application.Games.UploadGameMedia;
using KunigiArchive.Application.Media;
using KunigiArchive.Web.Contracts.Games;
using KunigiArchive.Web.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web.Controllers;

[ApiController]
[Route("api/games")]
[Produces("application/json")]
public sealed class GamesController : ControllerBase
{
    // Newest first, keyset-paged like the teams.
    [HttpGet]
    [ProducesResponseType<GamesPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> GetGames(
        [FromQuery] GetGamesRequest request,
        [FromServices] GetGamesHandler handler,
        CancellationToken cancellationToken)
    {
        GamesPage page = await handler.HandleAsync(request.ToQuery(), cancellationToken);
        return Ok(page.ToResponse());
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<GameSummaryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateGame(
        CreateGameRequest request,
        [FromServices] CreateGameHandler handler,
        CancellationToken cancellationToken)
    {
        GameSummary game = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return Created($"/api/games/{game.Id}", game.ToResponse());
    }

    // Corrects the admin's part of a game: year, number, host, winner.
    [HttpPut("{id:guid}/record")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<GameSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateGameRecord(
        Guid id,
        UpdateGameRecordRequest request,
        [FromServices] UpdateGameRecordHandler handler,
        CancellationToken cancellationToken)
    {
        GameSummary game = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return Ok(game.ToResponse());
    }

    // The host team's part: title, note, description. Admin or a manager of the host team.
    [HttpPut("{id:guid}/details")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType<GameDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateGameDetails(
        Guid id,
        UpdateGameDetailsRequest request,
        [FromServices] UpdateGameDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        GameDetails game = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return Ok(game.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteGame(
        Guid id,
        [FromServices] DeleteGameHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/image")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<GameDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> SetGameImage(
        Guid id,
        IFormFile image,
        [FromServices] SetGameImageHandler handler,
        CancellationToken cancellationToken)
    {
        await using Stream content = image.OpenReadStream();
        GameDetails game = await handler.HandleAsync(
            new SetGameImageCommand(id, new MediaUpload(content, image.FileName, image.Length)),
            cancellationToken);
        return Ok(game.ToResponse());
    }

    [HttpDelete("{id:guid}/image")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType<GameDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> RemoveGameImage(
        Guid id,
        [FromServices] SetGameImageHandler handler,
        CancellationToken cancellationToken)
    {
        GameDetails game = await handler.HandleAsync(new SetGameImageCommand(id, null), cancellationToken);
        return Ok(game.ToResponse());
    }

    [HttpPost("{id:guid}/media")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IReadOnlyList<GameMediaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UploadGameMedia(
        Guid id,
        IFormFileCollection files,
        [FromServices] UploadGameMediaHandler handler,
        CancellationToken cancellationToken)
    {
        List<MediaUpload> uploads = files.ToUploads();
        try
        {
            IReadOnlyList<GameMediaDetails> added =
                await handler.HandleAsync(new UploadGameMediaCommand(id, uploads), cancellationToken);
            return Ok(added.Select(media => media.ToResponse()).ToList());
        }
        finally
        {
            foreach (MediaUpload upload in uploads)
            {
                await upload.Content.DisposeAsync();
            }
        }
    }

    [HttpDelete("{id:guid}/media/{mediaId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteGameMedia(
        Guid id,
        Guid mediaId,
        [FromServices] DeleteGameMediaHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteGameMediaCommand(id, mediaId), cancellationToken);
        return NoContent();
    }

    // The guid routes above take precedence, so anything that isn't a guid lands here.
    [HttpGet("{slug}")]
    [ProducesResponseType<GameDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetGameBySlug(
        string slug,
        [FromServices] GetGameBySlugHandler handler,
        CancellationToken cancellationToken)
    {
        GameDetails game = await handler.HandleAsync(slug, cancellationToken);
        return Ok(game.ToResponse());
    }
}