using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class CreateGroupViewModel
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(100)]
        public string GroupName { get; set; } = string.Empty;
    }
}