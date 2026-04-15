using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class TutorProgressViewModel
    {
        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public string Note { get; set; } = string.Empty;

        [Range(0, 100)]
        public int ProgressPercent { get; set; }

        public bool IsIndividualFeedback { get; set; }
        public string? StudentId { get; set; }
    }
}