namespace Projectpath.Models
{
    public class GroupInvite
    {
        public int Id { get; set; }

        public int StudentGroupId { get; set; }
        public StudentGroup? StudentGroup { get; set; }

        public string InvitedStudentId { get; set; } = string.Empty;
        public ApplicationUser? InvitedStudent { get; set; }

        public string InvitedByStudentId { get; set; } = string.Empty;
        public ApplicationUser? InvitedByStudent { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}