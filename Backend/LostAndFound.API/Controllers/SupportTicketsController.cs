using LostAndFound.Api.Contracts.Support;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/support-tickets")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
public sealed class SupportTicketsController(ISupportTicketService supportTicketService) : ControllerBase
{
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
