using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Media;
using KunigiArchive.Application.Posts;
using KunigiArchive.Application.Posts.CreatePost;
using KunigiArchive.Application.Posts.DeletePost;
using KunigiArchive.Application.Posts.DeletePostMedia;
using KunigiArchive.Application.Posts.GetGamePosts;
using KunigiArchive.Application.Posts.UpdatePost;
using KunigiArchive.Application.Posts.UploadPostMedia;
using KunigiArchive.Web2.Contracts.Posts;
using KunigiArchive.Web2.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web2.Controllers;

// A game's posts. Writing them is for the admin and the host team's managers.
[ApiController]
[Produces("application/json")]
public sealed class PostsController : ControllerBase
{
    // Newest first.
    [HttpGet("api/games/{gameId:guid}/posts")]
    [ProducesResponseType<IReadOnlyList<PostResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGamePosts(
        Guid gameId,
        [FromServices] GetGamePostsHandler handler,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PostDetails> posts = await handler.HandleAsync(gameId, cancellationToken);
        return Ok(posts.Select(post => post.ToResponse()).ToList());
    }

    [HttpPost("api/games/{gameId:guid}/posts")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType<PostResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> CreatePost(
        Guid gameId,
        PostRequest request,
        [FromServices] CreatePostHandler handler,
        CancellationToken cancellationToken)
    {
        PostDetails post = await handler.HandleAsync(
            new CreatePostCommand(gameId, request.Title, request.Content), cancellationToken);
        return Created($"/api/games/{gameId}/posts", post.ToResponse());
    }

    [HttpPut("api/posts/{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType<PostResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdatePost(
        Guid id,
        PostRequest request,
        [FromServices] UpdatePostHandler handler,
        CancellationToken cancellationToken)
    {
        PostDetails post = await handler.HandleAsync(new UpdatePostCommand(id, request.Title, request.Content), cancellationToken);
        return Ok(post.ToResponse());
    }

    [HttpDelete("api/posts/{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeletePost(
        Guid id,
        [FromServices] DeletePostHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/posts/{id:guid}/media")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IReadOnlyList<PostMediaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UploadPostMedia(
        Guid id,
        IFormFileCollection files,
        [FromServices] UploadPostMediaHandler handler,
        CancellationToken cancellationToken)
    {
        List<MediaUpload> uploads = files.ToUploads();
        try
        {
            IReadOnlyList<PostMediaDetails> added =
                await handler.HandleAsync(new UploadPostMediaCommand(id, uploads), cancellationToken);
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

    [HttpDelete("api/posts/{id:guid}/media/{mediaId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeletePostMedia(
        Guid id,
        Guid mediaId,
        [FromServices] DeletePostMediaHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeletePostMediaCommand(id, mediaId), cancellationToken);
        return NoContent();
    }
}