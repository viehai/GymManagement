using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
    public class SystemLog
    {
        public int Id { get; set; }

        // NULL nếu lỗi hệ thống không gắn với user cụ thể
        public string? UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; }          // VD: "PaymentSuccess", "PackageCreated"

        [StringLength(100)]
        public string Entity { get; set; }           // VD: "MembershipPackage"

        [StringLength(50)]
        public string EntityId { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // Info / Warning / Error
        [Required]
        [StringLength(20)]
        public string Level { get; set; } = "Info";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}