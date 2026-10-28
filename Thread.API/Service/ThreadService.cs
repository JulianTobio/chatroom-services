using Microsoft.EntityFrameworkCore;
using Thread.API.Data;
using Thread.API.Models;
using ThreadModel = Thread.API.Models.Thread;

namespace Thread.API.Service;

public class ThreadService(
    ThreadContext context,
    IReplyApiClient replyApiClient,
    IUserProfilesClient userProfilesClient) : IThreadService
{
    public async Task<PagedResult<ThreadResponse>> GetAllAsync(int pageNumber, int pageSize)
    {
        var threads = context.Threads
            .OrderByDescending(thread => thread.Created)
            .ThenByDescending(thread => thread.Id);
        var totalCount = await threads.CountAsync();
        var offset = ((long)pageNumber - 1) * pageSize;
        var items = offset >= totalCount
            ? []
            : await threads.Skip((int)offset).Take(pageSize).ToListAsync();
        var profiles = await userProfilesClient.GetByIdsAsync(items.Select(thread => thread.Author));

        return new PagedResult<ThreadResponse>
        {
            Items = items.Select(thread => ToResponse(thread, profiles)).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task<ThreadModel?> GetAsync(int threadId)
    {
        return context.Threads.FirstOrDefaultAsync(thread => thread.Id == threadId);
    }

    public async Task<ThreadResponse?> GetResponseAsync(int threadId)
    {
        var thread = await GetAsync(threadId);
        if (thread is null)
        {
            return null;
        }

        var profiles = await userProfilesClient.GetByIdsAsync([thread.Author]);
        return ToResponse(thread, profiles);
    }

    public async Task<ThreadResponse> AddAsync(ThreadModel thread)
    {
        await context.Threads.AddAsync(thread);
        await context.SaveChangesAsync();
        var profiles = await userProfilesClient.GetByIdsAsync([thread.Author]);
        return ToResponse(thread, profiles);
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

    public async Task<ReplyApiReply?> CreateReplyAsync(
        int threadId,
        CreateThreadReplyRequest request)
    {
        var threadExists = await context.Threads
            .AnyAsync(thread => thread.Id == threadId);
        if (!threadExists)
        {
            return null;
        }

        var reply = await replyApiClient.CreateAsync(
            new ReplyApiCreateRequest
            {
                Content = request.Content,
                TopicId = threadId
            });
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

    public async Task<bool> DeleteReplyAsync(int threadId, int replyId, int userId)
    {
        var threadExists = await context.Threads
            .AnyAsync(thread => thread.Id == threadId);
        if (!threadExists)
        {
            return false;
        }

        var reply = await replyApiClient.GetAsync(replyId);
        if (reply is null || reply.TopicId != threadId || reply.UserId != userId)
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

    public async Task<IReadOnlyList<ReplyApiReply>?> GetRepliesAsync(int threadId)
    {
        var threadExists = await context.Threads
            .AnyAsync(thread => thread.Id == threadId);
        if (!threadExists)
        {
            return null;
        }

        const int pageSize = 100;
        var replies = new List<ReplyApiReply>();
        var firstPage = await replyApiClient.GetByThreadAsync(threadId, 1, pageSize);
        replies.AddRange(firstPage.Items);

        for (var pageNumber = 2; pageNumber <= firstPage.TotalPages; pageNumber++)
        {
            var page = await replyApiClient.GetByThreadAsync(threadId, pageNumber, pageSize);
            replies.AddRange(page.Items);
        }

        return replies
            .OrderBy(reply => reply.Date)
            .ThenBy(reply => reply.Id)
            .ToList();
    }

    private static ThreadResponse ToResponse(
        ThreadModel thread,
        IReadOnlyList<PublicUserProfile> profiles) =>
        new()
        {
            Id = thread.Id,
            Title = thread.Title,
            Description = thread.Description,
            Created = thread.Created,
            Author = thread.Author,
            AuthorProfile = profiles.FirstOrDefault(profile => profile.Id == thread.Author),
            ReplyCount = thread.ReplyCount,
            LastReplyTimestamp = thread.LastReplyTimestamp
        };
}
