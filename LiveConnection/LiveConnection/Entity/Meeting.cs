namespace LiveConnection.Entity
{
    public class Meeting
    {
        public int Id { get; set; }
        public int HostUserId { get; set; } 
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? MeetingCode { get; set; }

        public DateTime ScheduledTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public int MaxParticipants { get; set; } = 10;
        public bool IsPrivate { get; set; } = false;

        

        public bool IsScreenSharingActive { get; set; }
        public int? ScreenSharingUserId { get; set; }
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User HostUser { get; set; } = null!;
        public virtual ICollection<Participant> Participants { get; set; } = new List<Participant>();
        public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
        public virtual ICollection<MeetingFile> Files { get; set; } = new List<MeetingFile>();
    }
}
