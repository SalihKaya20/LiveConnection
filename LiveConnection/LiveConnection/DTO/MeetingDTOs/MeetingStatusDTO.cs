namespace LiveConnection.DTO
{
    public class MeetingStatusDTO
    {
        public int MeetingId { get; set; }
        public bool IsActive { get; set; }
        public bool IsScreenSharingActive { get; set; }
        public int? ScreenSharingUserId { get; set; }
        public DateTime LastActivityTime { get; set; }
        public List<UserStatusDTO>? Participants { get; set; }
    }
}