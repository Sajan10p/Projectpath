namespace Projectpath.Models
{
    public class GroupLeaveRequest
    {
        public int Id { get; set; }

        public int StudentGroupId { get; set; }
        public StudentGroup? StudentGroup { get; set; }

        public string StudentId { get; set; } = string.Empty;
        public ApplicationUser? Student { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}