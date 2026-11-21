using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

/// <summary>Conversations the user is not part of return 404, so their existence is not revealed.</summary>
[ApiController]
[Authorize]
[Route("api/conversations")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class ConversationsController(IConversationService conversationService) : ControllerBase
{
    /// <summary>
    /// "Message Owner": returns the existing conversation for the post, or starts a new one.
    /// 400 for your own post, 404 for a hidden or deleted post, 409 for a new chat on a resolved post.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ConversationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConversationResponse>> Start(
        StartConversationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await conversationService.StartAsync(request, cancellationToken));
    }

    /// <summary>Inbox, most recent activity first. A post owner sees a chat once it has a message.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<ConversationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ConversationResponse>>> GetAll(
        [FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        return Ok(await conversationService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ConversationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await conversationService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Newest messages first.</summary>
    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType<PagedResponse<MessageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<MessageResponse>>> GetMessages(
        Guid id, [FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        return Ok(await conversationService.GetMessagesAsync(id, query, cancellationToken));
    }

    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>> SendMessage(
        Guid id, SendMessageRequest request, CancellationToken cancellationToken)
    {
        MessageResponse message = await conversationService.SendMessageAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, message);
    }

    /// <summary>Marks the other participant's messages as read.</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await conversationService.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }
}
