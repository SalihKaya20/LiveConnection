namespace LiveConnection.DTO
{
    public class SendMessageDTO
{
    public int SenderId { get; set; }
    public int? ReceiverId { get; set; }
    public string Content { get; set; } = null!;
    public int? MeetingId { get; set; }
}

}