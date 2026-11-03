namespace Thread.API.Models;

public class ReplyApiCreateRequest
{
    public string Content { get; set; } = string.Empty;
    public int TopicId { get; set; }
}
