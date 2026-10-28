using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using ChatBot.API.Models;
using ChatBot.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp.Models.Exceptions;

namespace ChatBot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ChatController(
    IChatService chatService,
    ILogger<ChatController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ChatResponse>> Send(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Message)] = ["Message cannot be empty."]
                }));
        }

        if (request.ConversationId == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.ConversationId)] = ["ConversationId cannot be an empty GUID."]
                }));
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var conversationId = request.ConversationId ?? Guid.NewGuid();
        try
        {
            var response = await chatService.SendAsync(
                userId,
                conversationId,
                request.Message,
                cancellationToken);
            return Ok(response);
        }
        catch (HttpRequestException exception)
        {
            return ModelUnavailable(exception);
        }
        catch (OllamaException exception)
        {
            return ModelUnavailable(exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ModelUnavailable(exception);
        }
    }

    private ObjectResult ModelUnavailable(Exception exception)
    {
        logger.LogWarning(exception, "Ollama could not process a chat request.");
        return Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Chat model unavailable",
            detail: "The configured Ollama service could not process the request.");
    }
}
