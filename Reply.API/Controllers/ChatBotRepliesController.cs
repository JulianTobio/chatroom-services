using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Reply.API.Models;
using Reply.API.Service;

namespace Reply.API.Controllers;

[ApiController]
[Route("api/internal/chatbot/replies")]
public sealed class ChatBotRepliesController(
    IReplyService replyService,
    IUserProfilesClient userProfilesClient,
    IConfiguration configuration) : ControllerBase
{
    private const string InternalApiKeyHeader = "X-ChatBot-Service-Key";

    [HttpPost]
    [ProducesResponseType<ReplyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReplyResponse>> Create(
        CreateChatBotReplyRequest request)
    {
        if (!HasValidServiceKey())
        {
            return Unauthorized();
        }

        if (!int.TryParse(
                configuration["ChatBot:UserId"],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var chatBotUserId)
            || chatBotUserId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Chatbot identity is not configured",
                detail: "Configure ChatBot:UserId with the ID of the dedicated chatbot account in Users.API.");
        }

        IReadOnlyList<PublicUserProfile> profiles;
        try
        {
            profiles = await userProfilesClient.GetByIdsAsync([chatBotUserId]);
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Users.API request failed",
                detail: "The configured chatbot account could not be verified in Users.API.");
        }

        if (profiles.All(profile => profile.Id != chatBotUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Chatbot identity is not available",
                detail: "The configured ChatBot:UserId does not belong to an existing Users.API account.");
        }

        var reply = await replyService.AddChatBotReplyAsync(new ReplyItem
        {
            Content = request.Content,
            Date = DateTime.UtcNow,
            TopicId = request.TopicId,
            UserId = chatBotUserId
        });

        return CreatedAtAction(
            nameof(RepliesController.GetById),
            "Replies",
            new { replyId = reply.Id },
            reply);
    }

    private bool HasValidServiceKey()
    {
        var configuredKey = Encoding.UTF8.GetBytes(configuration["ChatBot:InternalApiKey"] ?? string.Empty);
        var providedKey = Encoding.UTF8.GetBytes(Request.Headers[InternalApiKeyHeader].ToString());
        return configuredKey.Length >= 32
            && providedKey.Length == configuredKey.Length
            && CryptographicOperations.FixedTimeEquals(providedKey, configuredKey);
    }
}
