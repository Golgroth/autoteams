using autoteams.Models;
using Microsoft.EntityFrameworkCore;

namespace autoteams
{
    public class StorageContext : DbContext
    {
        public DbSet<TeamsClassroom> Classrooms { get; set; }
        public DbSet<TeamsChannel> Channels { get; set; }
        public DbSet<ScheduledMeeting> Meetings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite(@"Data Source=storage.db;").EnableDetailedErrors().EnableSensitiveDataLogging();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamsClassroom>()
                .HasMany(c => c.Channels)
                .WithOne(c => c.Classroom)
                .HasForeignKey(c => c.ClassroomName);

            modelBuilder.Entity<TeamsChannel>()
                .HasMany(c => c.Meetings)
                .WithOne(c => c.Channel)
                .HasForeignKey(c => c.ChannelId);
        }
    }
}