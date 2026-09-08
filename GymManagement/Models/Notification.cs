using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        // Info / Warning / Success / Danger
        [Required]
        [StringLength(20)]
        public string Type { get; set; } = "Info";

        // Membership / Suspension / VipUpgrade / Payment / GymApproval / System
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "System";

        [StringLength(255)]
        public string? LinkUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = VnTime.Now;

        // Navigation property
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
