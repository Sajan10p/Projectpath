using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        [Required]
        public int StudentGroupId { get; set; }
        public StudentGroup? StudentGroup { get; set; }

        [Required]
        public string TutorId { get; set; } = string.Empty;
        public ApplicationUser? Tutor { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Assigned";

        public int CurrentProgressPercent { get; set; } = 0;

        public ICollection<ProgressUpdate> ProgressUpdates { get; set; } = new List<ProgressUpdate>();
    }
}