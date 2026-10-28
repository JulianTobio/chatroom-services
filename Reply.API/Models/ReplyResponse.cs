namespace Reply.API.Models;

public class ReplyResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int TopicId { get; set; }
    public int UserId { get; set; }
    public PublicUserProfile? UserProfile { get; set; }
}
