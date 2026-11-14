using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Media;
using KunigiArchive.Application.Teams;
using KunigiArchive.Application.Teams.AssignTeamManager;
using KunigiArchive.Application.Teams.CreateTeam;
using KunigiArchive.Application.Teams.DeleteTeamMedia;
using KunigiArchive.Application.Teams.GetTeamById;
using KunigiArchive.Application.Teams.GetTeamBySlug;
using KunigiArchive.Application.Teams.GetTeams;
using KunigiArchive.Application.Teams.RemoveTeamManager;
using KunigiArchive.Application.Teams.UpdateTeam;
using KunigiArchive.Application.Teams.UploadTeamMedia;
using KunigiArchive.Web2.Contracts.Teams;
using KunigiArchive.Web2.Mappers;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = Roles.Admin)]
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
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
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

    [HttpPost("{id:guid}/managers")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> AssignTeamManager(
        Guid id,
        AssignTeamManagerRequest request,
        [FromServices] AssignTeamManagerHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/managers/{userId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> RemoveTeamManager(
        Guid id,
        Guid userId,
        [FromServices] RemoveTeamManagerHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new RemoveTeamManagerCommand(id, userId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/media")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IReadOnlyList<TeamMediaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UploadTeamMedia(
        Guid id,
        IFormFileCollection files,
        [FromServices] UploadTeamMediaHandler handler,
        CancellationToken cancellationToken)
    {
        List<MediaUpload> uploads = files.ToUploads();
        try
        {
            IReadOnlyList<TeamMediaDetails> added =
                await handler.HandleAsync(new UploadTeamMediaCommand(id, uploads), cancellationToken);
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
    public async Task<IActionResult> DeleteTeamMedia(
        Guid id,
        Guid mediaId,
        [FromServices] DeleteTeamMediaHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteTeamMediaCommand(id, mediaId), cancellationToken);
        return NoContent();
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