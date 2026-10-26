using Microsoft.EntityFrameworkCore;
using Thread.API.Data;
using Thread.API.Models;
using ThreadModel = Thread.API.Models.Thread;

namespace Thread.API.Service;

public class ThreadService(ThreadContext context, IReplyApiClient replyApiClient) : IThreadService
{
    public async Task<PagedResult<ThreadModel>> GetAllAsync(int pageNumber, int pageSize)
    {
        var threads = context.Threads
            .OrderByDescending(thread => thread.Created)
            .ThenByDescending(thread => thread.Id);
        var totalCount = await threads.CountAsync();
        var offset = ((long)pageNumber - 1) * pageSize;
        var items = offset >= totalCount
            ? []
            : await threads.Skip((int)offset).Take(pageSize).ToListAsync();

        return new PagedResult<ThreadModel>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task<ThreadModel?> GetAsync(int threadId)
    {
        return context.Threads.FirstOrDefaultAsync(thread => thread.Id == threadId);
    }

    public async Task<ThreadModel> AddAsync(ThreadModel thread)
    {
        await context.Threads.AddAsync(thread);
        await context.SaveChangesAsync();
        return thread;
    }

    public async Task<bool> UpdateAsync(int threadId, ThreadModel thread)
    {
        var existingThread = await context.Threads.FindAsync(threadId);
        if (existingThread is null)
        {
            return false;
        }

        existingThread.Title = thread.Title;
        existingThread.Description = thread.Description;
        existingThread.Author = thread.Author;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveAsync(int threadId)
    {
        var thread = await context.Threads.FindAsync(threadId);
        if (thread is null)
        {
            return false;
        }

        context.Threads.Remove(thread);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<ReplyApiReply?> CreateReplyAsync(int threadId, CreateThreadReplyRequest request)
    {
        var threadExists = await context.Threads
            .AnyAsync(thread => thread.Id == threadId);
        if (!threadExists)
        {
            return null;
        }

        var reply = await replyApiClient.CreateAsync(threadId, request);
        var updatedRows = await context.Threads
            .Where(thread => thread.Id == threadId)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(thread => thread.ReplyCount, thread => thread.ReplyCount + 1)
                .SetProperty(
                    thread => thread.LastReplyTimestamp,
                    thread => thread.LastReplyTimestamp == null || thread.LastReplyTimestamp < reply.Date
                        ? reply.Date
                        : thread.LastReplyTimestamp));

        if (updatedRows == 0)
        {
            throw new InvalidOperationException(
                $"Reply {reply.Id} was created, but thread {threadId} no longer exists to update its reply metadata.");
        }

        return reply;
    }

    public async Task<bool> DeleteReplyAsync(int threadId, int replyId)
    {
        var threadExists = await context.Threads
            .AnyAsync(thread => thread.Id == threadId);
        if (!threadExists)
        {
            return false;
        }

        var reply = await replyApiClient.GetAsync(replyId);
        if (reply is null || reply.TopicId != threadId)
        {
            return false;
        }

        if (!await replyApiClient.DeleteAsync(replyId))
        {
            return false;
        }

        var remainingReplies = await replyApiClient.GetByThreadAsync(threadId, pageNumber: 1, pageSize: 1);
        var latestReplyTimestamp = remainingReplies.Items.Count == 0
            ? (DateTime?)null
            : remainingReplies.Items[0].Date;

        var updatedRows = await context.Threads
            .Where(thread => thread.Id == threadId)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(
                    thread => thread.ReplyCount,
                    thread => thread.ReplyCount > 0 ? thread.ReplyCount - 1 : 0)
                .SetProperty(
                    thread => thread.LastReplyTimestamp,
                    thread => thread.LastReplyTimestamp == reply.Date
                        ? latestReplyTimestamp
                        : thread.LastReplyTimestamp));

        if (updatedRows == 0)
        {
            throw new InvalidOperationException(
                $"Reply {replyId} was deleted, but thread {threadId} no longer exists to update its reply metadata.");
        }

        return true;
    }
}
