namespace ChatRoom.Models;

public class CreateReplyRequest
{
    public string Content { get; set; } = string.Empty;
    public int UserId { get; set; } = 1;
}
