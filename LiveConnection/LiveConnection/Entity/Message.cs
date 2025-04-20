namespace LiveConnection.Entity
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;



        public int? MeetingId { get; set; }
        public virtual Meeting Meeting {get ; set;} = null!;



        public int SenderId { get; set; }
        public virtual User? Sender { get; set; }

        public int? ReceiverId { get; set; }
        public virtual User? Receiver { get; set; }

        
        
    }
}