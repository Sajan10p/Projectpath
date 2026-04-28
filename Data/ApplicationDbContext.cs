using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Projectpath.Models;

namespace Projectpath.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<StudentGroup> StudentGroups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<GroupInvite> GroupInvites { get; set; }
        public DbSet<GroupJoinRequest> GroupJoinRequests { get; set; }
        public DbSet<GroupLeaveRequest> GroupLeaveRequests { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<ProgressUpdate> ProgressUpdates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Project>()
                .HasOne(p => p.Company)
                .WithMany()
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentGroup>()
                .HasOne(g => g.Project)
                .WithMany(p => p.StudentGroups)
                .HasForeignKey(g => g.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentGroup>()
                .HasOne(g => g.Leader)
                .WithMany()
                .HasForeignKey(g => g.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupMember>()
                .HasOne(m => m.StudentGroup)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.StudentGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMember>()
                .HasOne(m => m.Student)
                .WithMany()
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Assignment>()
                .HasOne(a => a.Project)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.ProjectId);

            builder.Entity<Submission>()
                .HasOne(s => s.Assignment)
                .WithMany()
                .HasForeignKey(s => s.AssignmentId);

            builder.Entity<Submission>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId);

            builder.Entity<ChatMessage>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChatMessage>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}