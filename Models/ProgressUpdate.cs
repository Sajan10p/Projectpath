using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class ProgressUpdate
    {
        public int Id { get; set; }

        [Required]
        public int AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }

        [Required]
        public string TutorId { get; set; } = string.Empty;
        public ApplicationUser? Tutor { get; set; }

        [Required]
        public string Note { get; set; } = string.Empty;

        public bool IsIndividualFeedback { get; set; } = false;

        public string? StudentId { get; set; }
        public ApplicationUser? Student { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}