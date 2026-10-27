using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Thread.API.Models;
using Thread.API.Service;
using ThreadModel = Thread.API.Models.Thread;

namespace Thread.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThreadsController(IThreadService threadService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ThreadModel>>> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10)
    {
        return Ok(await threadService.GetAllAsync(pageNumber, pageSize));
    }

    [HttpGet("{threadId:int}")]
    public async Task<ActionResult<ThreadModel>> GetById(int threadId)
    {
        var thread = await threadService.GetAsync(threadId);
        return thread is null ? NotFound() : Ok(thread);
    }

    [HttpGet("{threadId:int}/replies")]
    public async Task<IActionResult> GetReplies(int threadId)
    {
        try
        {
            var replies = await threadService.GetRepliesAsync(threadId);
            return replies is null ? NotFound() : Ok(replies);
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Reply.API request failed",
                detail: "The replies could not be loaded from Reply.API.");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ThreadModel>> Create(CreateThreadRequest request)
    {
        var thread = new ThreadModel
        {
            Title = request.Title,
            Description = request.Description,
            Created = DateTime.UtcNow,
            Author = request.Author
        };

        await threadService.AddAsync(thread);
        return CreatedAtAction(nameof(GetById), new { threadId = thread.Id }, thread);
    }

    [HttpPut("{threadId:int}")]
    public async Task<IActionResult> Update(int threadId, UpdateThreadRequest request)
    {
        var thread = new ThreadModel
        {
            Title = request.Title,
            Description = request.Description,
            Author = request.Author
        };

        return await threadService.UpdateAsync(threadId, thread) ? NoContent() : NotFound();
    }

    [HttpDelete("{threadId:int}")]
    public async Task<IActionResult> Delete(int threadId)
    {
        return await threadService.RemoveAsync(threadId) ? NoContent() : NotFound();
    }

    [HttpPost("{threadId:int}/replies")]
    public async Task<IActionResult> CreateReply(int threadId, CreateThreadReplyRequest request)
    {
        try
        {
            var reply = await threadService.CreateReplyAsync(threadId, request);
            return reply is null ? NotFound() : StatusCode(StatusCodes.Status201Created, reply);
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Reply.API request failed",
                detail: "The reply could not be created by Reply.API.");
        }
    }

    [HttpDelete("{threadId:int}/replies/{replyId:int}")]
    public async Task<IActionResult> DeleteReply(int threadId, int replyId)
    {
        try
        {
            return await threadService.DeleteReplyAsync(threadId, replyId)
                ? NoContent()
                : NotFound();
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Reply.API request failed",
                detail: "The reply could not be deleted or verified by Reply.API.");
        }
    }
}
