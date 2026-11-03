using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Reply.API.Models;
using Reply.API.Service;

namespace Reply.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepliesController(IReplyService replyService) : ControllerBase
    {
        [HttpGet("{replyId:int}")]
        public async Task<ActionResult<ReplyResponse>> GetById(int replyId)
        {
            try
            {
                var reply = await replyService.GetResponseAsync(replyId);
                return reply is null ? NotFound() : Ok(reply);
            }
            catch (HttpRequestException)
            {
                return ProfilesUnavailable();
            }
        }

        [HttpGet("topic/{topicId:int}")]
        public async Task<ActionResult<PagedResult<ReplyResponse>>> GetByTopic(
            int topicId,
            [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
            [FromQuery, Range(1, 100)] int pageSize = 10)
        {
            try
            {
                return Ok(await replyService.GetByTopicAsync(topicId, pageNumber, pageSize));
            }
            catch (HttpRequestException)
            {
                return ProfilesUnavailable();
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReplyResponse>> Create(CreateReplyRequest request)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var item = new ReplyItem
            {
                Content = request.Content,
                Date = DateTime.UtcNow,
                TopicId = request.TopicId,
                UserId = userId
            };

            try
            {
                var response = await replyService.AddReplyAsync(item);
                return CreatedAtAction(nameof(GetById), new { replyId = item.Id }, response);
            }
            catch (HttpRequestException)
            {
                return ProfilesUnavailable();
            }
        }

        [HttpPut("{replyId:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int replyId, ReplyItem item)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var updatedCount = await replyService.UpdateReplyAsync(replyId, userId, item.Content);
            return updatedCount == 0 ? NotFound() : NoContent();
        }

        [HttpDelete("{replyId:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int replyId)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var removed = await replyService.RemoveReplyAsync(replyId, userId);
            return removed ? NoContent() : NotFound();
        }

        private bool TryGetUserId(out int userId)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out userId);
        }

        private ObjectResult ProfilesUnavailable() =>
            Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Users.API request failed",
                detail: "The public user profiles could not be loaded from Users.API.");
    }
}
