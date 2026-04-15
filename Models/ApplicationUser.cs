using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Projectpath.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(50)]
        public string UserRole { get; set; } = string.Empty;

        [StringLength(30)]
        public string? StudentNumber { get; set; }

        [StringLength(30)]
        public string? TutorNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}