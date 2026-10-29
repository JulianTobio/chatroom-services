using Microsoft.EntityFrameworkCore;
using Reply.API.Data;
using Reply.API.Models;

namespace Reply.API.Service
{
    public class ReplyService(ReplyContext context, IUserProfilesClient userProfilesClient) : IReplyService
    {
        public Task<ReplyItem?> GetAsync(int replyId)
        {
            return context.Replies.FirstOrDefaultAsync(reply => reply.Id == replyId);
        }

        public async Task<ReplyResponse?> GetResponseAsync(int replyId)
        {
            var reply = await GetAsync(replyId);
            if (reply is null)
            {
                return null;
            }

            var profiles = await userProfilesClient.GetByIdsAsync([reply.UserId]);
            return ToResponse(reply, profiles);
        }

        public async Task<PagedResult<ReplyResponse>> GetByTopicAsync(int topicId, int pageNumber, int pageSize)
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
            var profiles = await userProfilesClient.GetByIdsAsync(items.Select(reply => reply.UserId));

            return new PagedResult<ReplyResponse>
            {
                Items = items.Select(reply => ToResponse(reply, profiles)).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ReplyResponse> AddReplyAsync(ReplyItem item)
        {
            await context.Replies.AddAsync(item);
            await context.SaveChangesAsync();
            var profiles = await userProfilesClient.GetByIdsAsync([item.UserId]);
            return ToResponse(item, profiles);
        }

        public async Task<ReplyResponse> AddChatBotReplyAsync(ReplyItem item)
        {
            await context.Replies.AddAsync(item);
            await context.SaveChangesAsync();
            return ToResponse(item, []);
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

        private static ReplyResponse ToResponse(
            ReplyItem reply,
            IReadOnlyList<PublicUserProfile> profiles) =>
            new()
            {
                Id = reply.Id,
                Content = reply.Content,
                Date = reply.Date,
                TopicId = reply.TopicId,
                UserId = reply.UserId,
                UserProfile = profiles.FirstOrDefault(profile => profile.Id == reply.UserId)
            };
    }
}
