namespace LiveConnection.DTO
{
    public class InviteFriendsDTO
    {
        public int MeetingId { get; set; }
        public List<int> FriendIds { get; set; } = new List<int>();
    }
}
