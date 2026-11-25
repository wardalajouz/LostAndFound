using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/users")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    /// <summary>Accounts that are not deleted, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<AdminUserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AdminUserResponse>>> Get(
        [FromQuery] AdminUsersQuery query, CancellationToken cancellationToken)
    {
        return Ok(await adminUserService.GetAsync(query, cancellationToken));
    }

    /// <summary>
    /// Disable or reactivate an account. The reason is logged, and a disabled user is signed out at once.
    /// 409 for your own account, another administrator, or a status the account already has.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        Guid id, UpdateAccountStatusRequest request, CancellationToken cancellationToken)
    {
        await adminUserService.UpdateStatusAsync(id, request, cancellationToken);
        return NoContent();
    }
}
