namespace LiveConnection.DTO
{
    public class SaveRealTimeMessageDTO
    {
        public int MeetingId { get; set; }
        public string Content { get; set; } = null!;
    }
}