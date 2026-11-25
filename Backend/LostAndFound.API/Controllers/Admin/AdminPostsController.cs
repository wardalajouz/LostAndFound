using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/posts")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminPostsController(IAdminPostService adminPostService) : ControllerBase
{
    /// <summary>All posts, including hidden and deleted ones. Use status=Resolved for Resolved Cases and handedToSecurity=true for Security Handover.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<AdminPostResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AdminPostResponse>>> Get(
        [FromQuery] AdminPostsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await adminPostService.GetAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminPostDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminPostDetailsResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await adminPostService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>
    /// Hide, soft-delete, or restore a post. The reason is logged and the owner is notified.
    /// 409 if the post already has that visibility, or its owner deleted their account and it would be shown again.
    /// </summary>
    [HttpPut("{id:guid}/visibility")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateVisibility(
        Guid id, UpdatePostVisibilityRequest request, CancellationToken cancellationToken)
    {
        await adminPostService.UpdateVisibilityAsync(id, request, cancellationToken);
        return NoContent();
    }
}
