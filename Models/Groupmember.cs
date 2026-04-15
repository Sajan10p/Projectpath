namespace Projectpath.Models
{
    public class GroupMember
    {
        public int Id { get; set; }

        public int StudentGroupId { get; set; }
        public StudentGroup? StudentGroup { get; set; }

        public string StudentId { get; set; } = string.Empty;
        public ApplicationUser? Student { get; set; }

        public bool IsLeader { get; set; } = false;
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }
}