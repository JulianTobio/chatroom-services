namespace Thread.API.Models;

public class ThreadResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public int Author { get; set; }
    public PublicUserProfile? AuthorProfile { get; set; }
    public int ReplyCount { get; set; }
    public DateTime? LastReplyTimestamp { get; set; }
}
