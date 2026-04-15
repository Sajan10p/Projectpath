using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class InviteStudentViewModel
    {
        [Required]
        public int StudentGroupId { get; set; }

        [Required]
        [Display(Name = "Student ID")]
        public string StudentNumber { get; set; } = string.Empty;
    }
}