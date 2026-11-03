using System.Security.Cryptography;
using System.Text;
using ChatBot.API.Models;
using ChatBot.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OllamaSharp.Models.Exceptions;

namespace ChatBot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ChatController(
    IChatService chatService,
    IOptions<ChatBotOptions> options,
    ILogger<ChatController> logger) : ControllerBase
{
    private const string InternalApiKeyHeader = "X-ChatBot-Service-Key";

    [HttpPost]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ChatResponse>> Send(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        return await GenerateAsync(request, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("internal")]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ChatResponse>> SendInternal(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        var configuredKey = Encoding.UTF8.GetBytes(options.Value.InternalApiKey);
        var providedKey = Encoding.UTF8.GetBytes(
            Request.Headers[InternalApiKeyHeader].ToString());
        if (providedKey.Length != configuredKey.Length
            || !CryptographicOperations.FixedTimeEquals(providedKey, configuredKey))
        {
            return Unauthorized();
        }

        return await GenerateAsync(request, cancellationToken);
    }

    private async Task<ActionResult<ChatResponse>> GenerateAsync(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await chatService.SendAsync(request.Message, cancellationToken));
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
