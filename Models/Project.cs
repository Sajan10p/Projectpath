using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Requirements { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Program { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ProjectType { get; set; } = string.Empty;

        [Required]
        public string CompanyId { get; set; } = string.Empty;
        public ApplicationUser? Company { get; set; }

        public bool IsApproved { get; set; } = false;

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? ContactPersonName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactPersonEmail { get; set; }
        public int? ExpectedDurationWeeks { get; set; }

        public string? ProjectFileName { get; set; }
        public string? ProjectFilePath { get; set; }

        public string? AdminInternalNotes { get; set; }
        public string? AdminDecisionMessage { get; set; }

        public ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}