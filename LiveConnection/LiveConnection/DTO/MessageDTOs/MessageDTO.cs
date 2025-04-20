namespace LiveConnection.DTO
{
    public class MessageDTO
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int? ReceiverId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; }
}

}