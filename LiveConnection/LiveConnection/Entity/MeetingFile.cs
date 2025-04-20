namespace LiveConnection.Entity
{
    public class MeetingFile
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; } = null!;
        public string FileUrl { get; set; } =null!;
        public DateTime UploadTime { get; set; }
        public long FileSize { get; set; }

        public virtual Meeting Meeting { get; set; }  = null!;
        public virtual User User { get; set; }  = null!;
    }
}
