using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me/posts")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class MyPostsController(IMyPostsService myPostsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<PostSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<PostSummaryResponse>>> Get(
        [FromQuery] MyPostsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await myPostsService.GetAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OwnPostResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnPostResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await myPostsService.GetByIdAsync(id, cancellationToken));
    }

    [HttpGet("stats")]
    [ProducesResponseType<PostStatsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PostStatsResponse>> GetStats(CancellationToken cancellationToken)
    {
        return Ok(await myPostsService.GetStatsAsync(cancellationToken));
    }
}
