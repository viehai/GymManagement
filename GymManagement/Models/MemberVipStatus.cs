using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    /// <summary>
    /// Lưu trữ trạng thái VIP và số lần tích lũy của từng Hội viên tại mỗi cơ sở phòng Gym (Module 3).
    /// </summary>
    public class MemberVipStatus
    {
        public int Id { get; set; }

        [Required]
        public string MemberId { get; set; } = string.Empty;

        [ForeignKey("MemberId")]
        public ApplicationUser Member { get; set; } = null!;

        [Required]
        public int GymId { get; set; }

        [ForeignKey("GymId")]
        public Gym Gym { get; set; } = null!;

        /// <summary>
        /// Hạng VIP hiện tại (null nếu chưa đạt mốc tối thiểu nào).
        /// </summary>
        public int? CurrentTierId { get; set; }

        [ForeignKey("CurrentTierId")]
        public VipTierSetting? CurrentTier { get; set; }

        /// <summary>
        /// Tổng số lần mua vé hoặc gia hạn vé thành công tại cơ sở này.
        /// </summary>
        public int TotalPurchaseCount { get; set; } = 0;

        /// <summary>
        /// Thời điểm đạt hạng VIP hiện tại.
        /// </summary>
        public DateTime? AchievedAt { get; set; }

        /// <summary>
        /// Thời điểm mua vé thành công gần nhất.
        /// </summary>
        public DateTime? LastPurchaseAt { get; set; }

        public DateTime CreatedAt { get; set; } = VnTime.Now;
    }
}
