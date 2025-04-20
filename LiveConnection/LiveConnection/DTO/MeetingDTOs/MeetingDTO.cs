namespace LiveConnection.DTO
{
    public class MeetingDTO
    {
        public int Id { get; set; }
        public int HostUserId { get; set; }
        public bool IsScreenSharingActive { get; set; }
        public int? ScreenSharingUserId { get; set; }
        public DateTime LastActivityTime { get; set; }
    }

}
