using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/posts")]
[Produces("application/json")]
public sealed class PostsController(IPostService postService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<PostResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<PostResponse>> Create(
        CreatePostRequest request, CancellationToken cancellationToken)
    {
        PostResponse post = await postService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, post);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<PostResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<PostResponse>> Update(
        Guid id, UpdatePostRequest request, CancellationToken cancellationToken)
    {
        return Ok(await postService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await postService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
