using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/stats")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
public sealed class AdminStatsController(IAdminStatsService adminStatsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AdminStatsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminStatsResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await adminStatsService.GetAsync(cancellationToken));
    }
}
