using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class StudentGroup
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        [Required]
        [StringLength(100)]
        public string GroupName { get; set; } = string.Empty;

        [Required]
        public string LeaderId { get; set; } = string.Empty;
        public ApplicationUser? Leader { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Open";

        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<GroupInvite> Invites { get; set; } = new List<GroupInvite>();
    }
}