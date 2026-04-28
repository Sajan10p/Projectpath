using System;
using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class Submission
    {
        public int Id { get; set; }

        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }

        public string StudentId { get; set; }
        public ApplicationUser Student { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}