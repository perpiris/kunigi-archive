using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.GameDays;
using KunigiArchive.Application.GameDays.GetGameDays;
using KunigiArchive.Application.GameDays.ManageGameDay;
using KunigiArchive.Application.GameDays.ManagePuzzles;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.GameDays;
using KunigiArchive.Web2.Contracts.GameDays;
using KunigiArchive.Web2.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web2.Controllers;

// A game's days and their puzzles. Reading enabled days is public; changes are for the admin and the host
// team's managers. A day is addressed by its game and slug ("sabbato") for reading, by its id for changes.
[ApiController]
[Produces("application/json")]
public sealed class GameDaysController : ControllerBase
{
    [HttpGet("api/games/{gameId:guid}/days")]
    [ProducesResponseType<IReadOnlyList<GameDaySummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDays(Guid gameId, [FromServices] GetGameDaysHandler handler, CancellationToken cancellationToken)
    {
        IReadOnlyList<GameDaySummary> days = await handler.HandleAsync(gameId, cancellationToken);
        return Ok(days.Select(day => day.ToResponse()).ToList());
    }

    [HttpGet("api/games/{gameId:guid}/days/{day}")]
    [ProducesResponseType<GameDayResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetDay(Guid gameId, string day, [FromServices] GetGameDayHandler handler, CancellationToken cancellationToken)
    {
        GameDayPage page = await handler.HandleAsync(gameId, day, cancellationToken);
        return Ok(page.Day.ToResponse());
    }

    // The first time a day is enabled, it's created.
    [HttpPut("api/games/{gameId:guid}/days/{day}/enabled")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType<GameDaySummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> SetEnabled(
        Guid gameId,
        string day,
        SetGameDayEnabledRequest request,
        [FromServices] SetGameDayEnabledHandler handler,
        CancellationToken cancellationToken)
    {
        if (!GameDayTypes.TryParseSlug(day, out GameDayType? type))
        {
            throw new GameDayNotFoundException(day);
        }

        GameDaySummary summary = await handler.HandleAsync(new SetGameDayEnabledCommand(gameId, type.Value, request.Enabled), cancellationToken);
        return Ok(summary.ToResponse());
    }

    [HttpPut("api/game-days/{dayId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType<GameDayResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateDay(
        Guid dayId,
        GameDayRequest request,
        [FromServices] UpdateGameDayHandler handler,
        CancellationToken cancellationToken)
    {
        GameDayDetails day = await handler.HandleAsync(new UpdateGameDayCommand(dayId, request.Title, request.Description), cancellationToken);
        return Ok(day.ToResponse());
    }

    [HttpPost("api/game-days/{dayId:guid}/media")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IReadOnlyList<MediaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UploadDayMedia(
        Guid dayId,
        IFormFileCollection files,
        [FromServices] UploadGameDayMediaHandler handler,
        CancellationToken cancellationToken)
    {
        return await UploadAsync(files, uploads => handler.HandleAsync(new UploadGameDayMediaCommand(dayId, uploads), cancellationToken));
    }

    [HttpDelete("api/game-days/{dayId:guid}/media/{mediaId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteDayMedia(
        Guid dayId,
        Guid mediaId,
        [FromServices] DeleteGameDayMediaHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteGameDayMediaCommand(dayId, mediaId), cancellationToken);
        return NoContent();
    }

    // A new puzzle goes last.
    [HttpPost("api/game-days/{dayId:guid}/puzzles")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType<PuzzleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> CreatePuzzle(
        Guid dayId,
        PuzzleRequest request,
        [FromServices] CreatePuzzleHandler handler,
        CancellationToken cancellationToken)
    {
        PuzzleDetails puzzle = await handler.HandleAsync(
            new PuzzleCommand(dayId, Guid.Empty, request.Group, request.Question, request.Answer), cancellationToken);
        return Created($"/api/game-days/{dayId}/puzzles/{puzzle.Id}", puzzle.ToResponse());
    }

    [HttpPut("api/game-days/{dayId:guid}/puzzles/{puzzleId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType<PuzzleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdatePuzzle(
        Guid dayId,
        Guid puzzleId,
        PuzzleRequest request,
        [FromServices] UpdatePuzzleHandler handler,
        CancellationToken cancellationToken)
    {
        PuzzleDetails puzzle = await handler.HandleAsync(
            new PuzzleCommand(dayId, puzzleId, request.Group, request.Question, request.Answer), cancellationToken);
        return Ok(puzzle.ToResponse());
    }

    // The puzzles after it move up one; a package left with one puzzle is dissolved.
    [HttpDelete("api/game-days/{dayId:guid}/puzzles/{puzzleId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeletePuzzle(
        Guid dayId,
        Guid puzzleId,
        [FromServices] DeletePuzzleHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new PuzzleRef(dayId, puzzleId), cancellationToken);
        return NoContent();
    }

    [HttpPost("api/game-days/{dayId:guid}/puzzles/{puzzleId:guid}/move")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> MovePuzzle(
        Guid dayId,
        Guid puzzleId,
        MovePuzzleRequest request,
        [FromServices] MovePuzzleHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new MovePuzzleCommand(dayId, puzzleId, request.Up), cancellationToken);
        return NoContent();
    }

    // kind: "question" or "answer".
    [HttpPost("api/game-days/{dayId:guid}/puzzles/{puzzleId:guid}/media")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IReadOnlyList<MediaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UploadPuzzleMedia(
        Guid dayId,
        Guid puzzleId,
        [FromQuery] string kind,
        IFormFileCollection files,
        [FromServices] UploadPuzzleMediaHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(kind, ignoreCase: true, out PuzzleMediaKind mediaKind) || !Enum.IsDefined(mediaKind) || int.TryParse(kind, out _))
        {
            throw new InvalidRequestException("Το kind πρέπει να είναι \"question\" ή \"answer\".");
        }

        return await UploadAsync(files, uploads =>
            handler.HandleAsync(new UploadPuzzleMediaCommand(dayId, puzzleId, mediaKind, uploads), cancellationToken));
    }

    [HttpDelete("api/game-days/{dayId:guid}/puzzles/{puzzleId:guid}/media/{mediaId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeletePuzzleMedia(
        Guid dayId,
        Guid puzzleId,
        Guid mediaId,
        [FromServices] DeletePuzzleMediaHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeletePuzzleMediaCommand(dayId, puzzleId, mediaId), cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult> UploadAsync(
        IFormFileCollection files,
        Func<List<MediaUpload>, Task<IReadOnlyList<MediaDetails>>> upload)
    {
        List<MediaUpload> uploads = files.ToUploads();
        try
        {
            IReadOnlyList<MediaDetails> added = await upload(uploads);
            return Ok(added.Select(media => media.ToResponse()).ToList());
        }
        finally
        {
            foreach (MediaUpload item in uploads)
            {
                await item.Content.DisposeAsync();
            }
        }
    }
}
