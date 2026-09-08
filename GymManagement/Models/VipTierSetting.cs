using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    /// <summary>
    /// Cấu hình các hạng VIP do Chủ phòng Gym (Owner) thiết lập cho từng cơ sở (Module 3).
    /// </summary>
    public class VipTierSetting
    {
        public int Id { get; set; }

        [Required]
        public int GymId { get; set; }

        [ForeignKey("GymId")]
        public Gym Gym { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập tên hạng VIP.")]
        [StringLength(50)]
        public string TierName { get; set; } = string.Empty;

        /// <summary>
        /// Số lần mua vé/gia hạn tối thiểu để đạt được hạng này tại Gym.
        /// </summary>
        [Required]
        [Range(1, 10000, ErrorMessage = "Số lần mua tối thiểu phải lớn hơn hoặc bằng 1.")]
        public int MinPurchaseCount { get; set; } = 5;

        /// <summary>
        /// % giảm giá tự động khi mua vé mới hoặc gia hạn vé (0 - 100%).
        /// </summary>
        [Range(0, 100, ErrorMessage = "% giảm giá từ 0% đến 100%.")]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? DiscountPercent { get; set; }

        /// <summary>
        /// Mô tả đặc quyền, phần thưởng hoặc ưu đãi đi kèm.
        /// </summary>
        [StringLength(500)]
        public string? BenefitDescription { get; set; }

        /// <summary>
        /// Mã màu hiển thị cho badge (VD: #C0C0C0 cho Silver, #FFD700 cho Gold, #6366F1...).
        /// </summary>
        [Required]
        [StringLength(20)]
        public string BadgeColor { get; set; } = "#C0C0C0";

        /// <summary>
        /// Thứ tự cấp bậc (1 là thấp nhất, tăng dần).
        /// </summary>
        public int DisplayOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = VnTime.Now;

        // Navigation
        public ICollection<MemberVipStatus> MemberVipStatuses { get; set; } = new List<MemberVipStatus>();
    }
}
