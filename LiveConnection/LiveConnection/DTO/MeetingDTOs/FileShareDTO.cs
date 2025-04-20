namespace LiveConnection.DTO
{
    public class FileShareDTO
    {
        public int UserId { get; set; }
        public string FileName { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
        public long FileSize { get; set; }
        public DateTime UploadTime { get; set; }
    }
}
