namespace LiveConnection.DTO
{
    public class UserStatusDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public bool IsMuted { get; set; }
        public bool IsVideoEnabled { get; set; }
        public bool IsScreenSharing { get; set; }
    }

}
