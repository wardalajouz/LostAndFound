using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/stats")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminStatsController(IAdminStatsService adminStatsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AdminStatsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminStatsResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await adminStatsService.GetAsync(cancellationToken));
    }
}
