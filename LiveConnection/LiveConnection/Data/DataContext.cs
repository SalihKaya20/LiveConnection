using Microsoft.EntityFrameworkCore;
using LiveConnection.Entity;

namespace LiveConnection.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Message> Messages {get; set;}
        public DbSet<MeetingFile> MeetingFiles { get; set; }

        
        
    }
}
