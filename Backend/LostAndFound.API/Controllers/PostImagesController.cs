using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/posts/{postId:guid}/images")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
public sealed class PostImagesController(IPostImageService postImageService) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IReadOnlyList<PostImageResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PostImageResponse>>> Upload(
        Guid postId, [FromForm] UploadPostImagesRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<PostImageResponse> images = await postImageService.UploadAsync(postId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, images);
    }

    [HttpDelete("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid postId, Guid imageId, CancellationToken cancellationToken)
    {
        await postImageService.DeleteAsync(postId, imageId, cancellationToken);
        return NoContent();
    }
}
