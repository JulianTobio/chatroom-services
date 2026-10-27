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
        public async Task<ActionResult<ReplyItem>> GetById(int replyId)
        {
            var reply = await replyService.GetAsync(replyId);
            return reply is null ? NotFound() : Ok(reply);
        }

        [HttpGet("topic/{topicId:int}")]
        public async Task<ActionResult<PagedResult<ReplyItem>>> GetByTopic(
            int topicId,
            [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
            [FromQuery, Range(1, 100)] int pageSize = 10)
        {
            var result = await replyService.GetByTopicAsync(topicId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReplyItem>> Create(CreateReplyRequest request)
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

            await replyService.AddReplyAsync(item);
            return CreatedAtAction(nameof(GetById), new { replyId = item.Id }, item);
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
    }
}
