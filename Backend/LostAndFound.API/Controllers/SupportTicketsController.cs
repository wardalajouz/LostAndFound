using LostAndFound.Api.Contracts.Support;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/support-tickets")]
[Produces("application/json")]
public sealed class SupportTicketsController(ISupportTicketService supportTicketService) : ControllerBase
{
    /// <summary>Open to guests. Signed-in users' tickets are linked to their account. A copy is emailed to the support inbox.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        CreateSupportTicketRequest request, CancellationToken cancellationToken)
    {
        await supportTicketService.CreateAsync(request, cancellationToken);
        return Accepted();
    }
}
