namespace LiveConnection.Entity
{
    public class Participant
    {
        public int Id { get; set; }


        public int MeetingId { get; set; }
        public virtual Meeting Meeting { get; set; } = null!;


        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;


        public bool IsMuted {get ; set;} = false;
        public bool IsVideoEnabled {get ; set;} = true;
        public bool IsHost {get ; set ;} = false;
    }
}