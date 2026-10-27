namespace Reply.API.Models
{
    public class CreateReplyRequest
    {
        public string Content { get; set; } = string.Empty;
        public int TopicId { get; set; }
    }
}
