using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    /// <summary>
    /// Lưu trữ lịch sử điểm danh / check-in của Hội viên khi đến phòng tập (Module 4).
    /// Hỗ trợ cơ chế Session Timeout (Tự động bốc hơi sau 2 giờ) và Check-out sớm.
    /// </summary>
    public class CheckinLog
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
        /// Vé tập / Gói tập đang hoạt động được sử dụng trong lượt check-in này (nếu có).
        /// </summary>
        public int? MembershipId { get; set; }

        [ForeignKey("MembershipId")]
        public MemberMembership? Membership { get; set; }

        /// <summary>
        /// Thời điểm hội viên check-in vào phòng tập.
        /// </summary>
        public DateTime CheckinTime { get; set; } = VnTime.Now;

        /// <summary>
        /// Thời điểm check-out (null nếu áp dụng cơ chế tự động bốc hơi 2 tiếng, hoặc có giá trị nếu lễ tân check-out sớm).
        /// </summary>
        public DateTime? CheckoutTime { get; set; }

        /// <summary>
        /// Tài khoản Lễ tân / Chủ phòng đã quét hoặc xác nhận lượt check-in này.
        /// </summary>
        public string? CheckedByUserId { get; set; }

        [ForeignKey("CheckedByUserId")]
        public ApplicationUser? CheckedByUser { get; set; }

        /// <summary>
        /// Trạng thái check-in: Success (Hợp lệ vào tập), Expired (Hết hạn), Suspended (Bị đình chỉ), NoActiveMembership (Không có vé).
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Success";

        /// <summary>
        /// Phương thức check-in: QrScan (Quét camera), ManualPhone (Nhập SĐT), UsbScanner (Súng quét USB).
        /// </summary>
        [Required]
        [StringLength(30)]
        public string CheckinMethod { get; set; } = "QrScan";

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
