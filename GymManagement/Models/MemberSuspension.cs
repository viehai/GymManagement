using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    public class MemberSuspension
    {
        public int Id { get; set; }

        [Required]
        public int GymId { get; set; }

        [ForeignKey("GymId")]
        public Gym Gym { get; set; } = null!;

        [Required]
        public string MemberId { get; set; } = string.Empty;

        [ForeignKey("MemberId")]
        public ApplicationUser Member { get; set; } = null!;

        [Required]
        public string SuspendedByUserId { get; set; } = string.Empty;

        [ForeignKey("SuspendedByUserId")]
        public ApplicationUser SuspendedByUser { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// "Temporary" (tạm thời) hoặc "Permanent" (vĩnh viễn).
        /// </summary>
        [Required]
        [StringLength(20)]
        public string SuspensionType { get; set; } = "Temporary";

        public DateTime StartDate { get; set; } = VnTime.Now;

        /// <summary>
        /// Ngày kết thúc đình chỉ. Null nếu là Permanent.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// "Active" (đang hiệu lực) hoặc "Lifted" (đã gỡ bỏ).
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime? LiftedAt { get; set; }

        [StringLength(500)]
        public string? LiftedReason { get; set; }

        public DateTime CreatedAt { get; set; } = VnTime.Now;
    }
}
