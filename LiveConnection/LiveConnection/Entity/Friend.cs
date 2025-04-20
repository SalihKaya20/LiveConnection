using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveConnection.Entity
{
    public class Friend
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        [ForeignKey("FriendUser")]
        public int FriendId { get; set; }
        public virtual User FriendUser { get; set; } = null!;


        public bool IsAccepted { get; set; }
    }
}