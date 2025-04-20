namespace LiveConnection.DTO
{
    public class InvitationDTO
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public string MeetingTitle { get; set; } = null!;
        public int InvitedUserId { get; set; }
        public string InvitedUsername { get; set; } = null!;
        public int SenderId { get; set; }
        public string SenderUsername { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
} 