using KunigiArchive.Application.Teams;
using KunigiArchive.Application.Teams.CreateTeam;
using KunigiArchive.Application.Teams.GetTeamById;
using KunigiArchive.Application.Teams.GetTeamBySlug;
using KunigiArchive.Application.Teams.GetTeams;
using KunigiArchive.Application.Teams.UpdateTeam;
using KunigiArchive.Web2.Contracts.Teams;
using KunigiArchive.Web2.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web2.Controllers;

[ApiController]
[Route("api/teams")]
[Produces("application/json")]
public sealed class TeamsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TeamsPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> GetTeams(
        [FromQuery] GetTeamsRequest request,
        [FromServices] GetTeamsHandler handler,
        CancellationToken cancellationToken)
    {
        TeamsPage page = await handler.HandleAsync(request.ToQuery(), cancellationToken);
        return Ok(page.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateTeam(
        CreateTeamRequest request,
        [FromServices] CreateTeamHandler handler,
        CancellationToken cancellationToken)
    {
        TeamDetails team = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(GetTeamById), new { id = team.Id }, team.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTeamById(
        Guid id,
        [FromServices] GetTeamByIdHandler handler,
        CancellationToken cancellationToken)
    {
        TeamDetails team = await handler.HandleAsync(id, cancellationToken);
        return Ok(team.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateTeam(
        Guid id,
        UpdateTeamRequest request,
        [FromServices] UpdateTeamHandler handler,
        CancellationToken cancellationToken)
    {
        TeamDetails team = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return Ok(team.ToResponse());
    }

    // The guid constraint above takes precedence, so anything that isn't a guid lands here.
    [HttpGet("{slug}")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTeamBySlug(
        string slug,
        [FromServices] GetTeamBySlugHandler handler,
        CancellationToken cancellationToken)
    {
        TeamDetails team = await handler.HandleAsync(slug, cancellationToken);
        return Ok(team.ToResponse());
    }
}