using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Thread.API.Models;
using Thread.API.Service;
using ThreadModel = Thread.API.Models.Thread;

namespace Thread.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThreadsController(
    IThreadService threadService,
    IThreadUpdatesPublisher threadUpdatesPublisher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ThreadResponse>>> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10)
    {
        try
        {
            return Ok(await threadService.GetAllAsync(pageNumber, pageSize));
        }
        catch (HttpRequestException)
        {
            return ProfilesUnavailable();
        }
    }

    [HttpGet("{threadId:int}")]
    public async Task<ActionResult<ThreadResponse>> GetById(int threadId)
    {
        try
        {
            var thread = await threadService.GetResponseAsync(threadId);
            return thread is null ? NotFound() : Ok(thread);
        }
        catch (HttpRequestException)
        {
            return ProfilesUnavailable();
        }
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
    [Authorize]
    public async Task<ActionResult<ThreadResponse>> Create(CreateThreadRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var thread = new ThreadModel
        {
            Title = request.Title,
            Description = request.Description,
            Created = DateTime.UtcNow,
            Author = userId
        };

        try
        {
            var response = await threadService.AddAsync(thread);
            await threadUpdatesPublisher.PublishAsync(thread.Id, "thread-created");
            return CreatedAtAction(nameof(GetById), new { threadId = thread.Id }, response);
        }
        catch (HttpRequestException)
        {
            return ProfilesUnavailable();
        }
    }

    [HttpPut("{threadId:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int threadId, UpdateThreadRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var existingThread = await threadService.GetAsync(threadId);
        if (existingThread is null)
        {
            return NotFound();
        }
        if (existingThread.Author != userId)
        {
            return Forbid();
        }

        var thread = new ThreadModel
        {
            Title = request.Title,
            Description = request.Description
        };

        if (!await threadService.UpdateAsync(threadId, thread))
        {
            return NotFound();
        }

        await threadUpdatesPublisher.PublishAsync(threadId, "thread-updated");
        return NoContent();
    }

    [HttpDelete("{threadId:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int threadId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var thread = await threadService.GetAsync(threadId);
        if (thread is null)
        {
            return NotFound();
        }
        if (thread.Author != userId)
        {
            return Forbid();
        }

        if (!await threadService.RemoveAsync(threadId))
        {
            return NotFound();
        }

        await threadUpdatesPublisher.PublishAsync(threadId, "thread-deleted");
        return NoContent();
    }

    [HttpPost("{threadId:int}/replies")]
    [Authorize]
    public async Task<IActionResult> CreateReply(int threadId, CreateThreadReplyRequest request)
    {
        if (!TryGetUserId(out _))
        {
            return Unauthorized();
        }

        try
        {
            var reply = await threadService.CreateReplyAsync(
                threadId,
                new CreateThreadReplyRequest { Content = request.Content });
            if (reply is not null)
            {
                await threadUpdatesPublisher.PublishAsync(threadId, "reply-created");
            }

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
    [Authorize]
    public async Task<IActionResult> DeleteReply(int threadId, int replyId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            if (!await threadService.DeleteReplyAsync(threadId, replyId, userId))
            {
                return NotFound();
            }

            await threadUpdatesPublisher.PublishAsync(threadId, "reply-deleted");
            return NoContent();
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Reply.API request failed",
                detail: "The reply could not be deleted or verified by Reply.API.");
        }
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
