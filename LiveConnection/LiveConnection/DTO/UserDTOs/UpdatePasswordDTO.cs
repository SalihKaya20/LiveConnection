namespace LiveConnection.DTO
{
    public class UpdatePasswordDTO
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        
    }
}