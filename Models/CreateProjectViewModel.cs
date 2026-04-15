using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class CreateProjectViewModel
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Requirements { get; set; } = string.Empty;

        public string? ContactPersonName { get; set; }
        public string? ContactPhone { get; set; }

        [EmailAddress]
        [Display(Name = "Contact Person Email")]
        public string? ContactPersonEmail { get; set; }

        public int? ExpectedDurationWeeks { get; set; }
    }
}