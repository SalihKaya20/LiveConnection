namespace LiveConnection.Entity
{
    public class Invitation
    {
        public int Id { get; set; }
        public int MeetingId { get; set; } // Hangi toplantıya davet edildi
        public int InvitedUserId { get; set; } // Davet edilen kişi
        public int SenderId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Meeting Meeting { get; set; } = null!;
        public virtual User InvitedUser { get; set; } = null!;
        public virtual User Sender { get; set; } = null!;
    }
}
