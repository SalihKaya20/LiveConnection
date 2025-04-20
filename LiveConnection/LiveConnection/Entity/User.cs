namespace LiveConnection.Entity
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

        public virtual ICollection<Participant> Participants {get ; set;} = new List<Participant>();
        
        public virtual ICollection<MeetingFile> MeetingFiles { get; set; } = new List<MeetingFile>();



        public string Status { get; set; } = "Offline";
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}