using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class AssignProjectViewModel
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int StudentGroupId { get; set; }

        [Required]
        public string TutorId { get; set; } = string.Empty;
    }
}