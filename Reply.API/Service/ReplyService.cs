using Microsoft.EntityFrameworkCore;
using Reply.API.Data;
using Reply.API.Models;

namespace Reply.API.Service
{
    public class ReplyService(ReplyContext context) : IReplyService
    {
        public Task<ReplyItem?> GetAsync(int replyId)
        {
            return context.Replies.FirstOrDefaultAsync(reply => reply.Id == replyId);
        }

        public async Task<PagedResult<ReplyItem>> GetByTopicAsync(int topicId, int pageNumber, int pageSize)
        {
            var topicReplies = context.Replies
                .Where(reply => reply.TopicId == topicId)
                .OrderByDescending(reply => reply.Date)
                .ThenByDescending(reply => reply.Id);
            var totalCount = await topicReplies.CountAsync();
            var offset = ((long)pageNumber - 1) * pageSize;
            var items = offset >= totalCount
                ? []
                : await topicReplies.Skip((int)offset).Take(pageSize).ToListAsync();

            return new PagedResult<ReplyItem>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task AddReplyAsync(ReplyItem item)
        {
            await context.Replies.AddAsync(item);
            await context.SaveChangesAsync();
        }

        public async Task<bool> RemoveReplyAsync(int replyId, int userId)
        {
            var reply = await context.Replies.FindAsync(replyId);
            if (reply is null || reply.UserId != userId)
            {
                return false;
            }

            context.Replies.Remove(reply);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<int> UpdateReplyAsync(int replyId, int userId, string content)
        {
            var existingReply = await context.Replies.FindAsync(replyId);
            if (existingReply is null || existingReply.UserId != userId)
            {
                return 0;
            }

            existingReply.Content = content;

            return await context.SaveChangesAsync();
        }
    }
}
