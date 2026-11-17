using LostAndFound.Api.Contracts.CampusLocations;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/campus-locations")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
public sealed class CampusLocationsController(ICampusLocationService campusLocationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CampusLocationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CampusLocationResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await campusLocationService.GetAllAsync(cancellationToken));
    }
}
