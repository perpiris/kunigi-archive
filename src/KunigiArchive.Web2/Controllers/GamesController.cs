using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Games.CreateGame;
using KunigiArchive.Application.Games.GetGames;
using KunigiArchive.Application.Games.UpdateGameRecord;
using KunigiArchive.Web2.Contracts.Games;
using KunigiArchive.Web2.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web2.Controllers;

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
}