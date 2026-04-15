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

            builder.Entity<GroupInvite>()
                .HasOne(i => i.StudentGroup)
                .WithMany(g => g.Invites)
                .HasForeignKey(i => i.StudentGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupInvite>()
                .HasOne(i => i.InvitedStudent)
                .WithMany()
                .HasForeignKey(i => i.InvitedStudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupInvite>()
                .HasOne(i => i.InvitedByStudent)
                .WithMany()
                .HasForeignKey(i => i.InvitedByStudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupJoinRequest>()
                .HasOne(r => r.StudentGroup)
                .WithMany()
                .HasForeignKey(r => r.StudentGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupJoinRequest>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupLeaveRequest>()
                .HasOne(r => r.StudentGroup)
                .WithMany()
                .HasForeignKey(r => r.StudentGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupLeaveRequest>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Assignment>()
                .HasOne(a => a.Project)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Assignment>()
                .HasOne(a => a.StudentGroup)
                .WithMany()
                .HasForeignKey(a => a.StudentGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Assignment>()
                .HasOne(a => a.Tutor)
                .WithMany()
                .HasForeignKey(a => a.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProgressUpdate>()
                .HasOne(p => p.Assignment)
                .WithMany(a => a.ProgressUpdates)
                .HasForeignKey(p => p.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProgressUpdate>()
                .HasOne(p => p.Tutor)
                .WithMany()
                .HasForeignKey(p => p.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProgressUpdate>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}