namespace LiveConnection.DTO
{
    public class CreateMeetingDTO
    {
        public int HostUserId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Title { get; set; } = null!;
    }
}
