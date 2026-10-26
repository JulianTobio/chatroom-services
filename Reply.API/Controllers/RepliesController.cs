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
        public async Task<ActionResult<ReplyItem>> Create(CreateReplyRequest request)
        {
            var item = new ReplyItem
            {
                Content = request.Content,
                Date = DateTime.UtcNow,
                TopicId = request.TopicId,
                UserId = request.UserId
            };

            await replyService.AddReplyAsync(item);
            return CreatedAtAction(nameof(GetById), new { replyId = item.Id }, item);
        }

        [HttpPut("{replyId:int}")]
        public async Task<IActionResult> Update(int replyId, ReplyItem item)
        {
            var updatedCount = await replyService.UpdateReplyAsync(replyId, item);
            return updatedCount == 0 ? NotFound() : NoContent();
        }

        [HttpDelete("{replyId:int}")]
        public async Task<IActionResult> Delete(int replyId)
        {
            var removed = await replyService.RemoveReplyAsync(replyId);
            return removed ? NoContent() : NotFound();
        }
    }
}
