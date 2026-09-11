using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    /// <summary>
    /// Lưu trữ đánh giá sao và nhận xét của hội viên đối với phòng gym đã từng mua vé.
    /// </summary>
    public class GymReview
    {
        public int Id { get; set; }

        [Required]
        public int GymId { get; set; }

        [Required]
        public string MemberId { get; set; } = string.Empty;

        /// <summary>
        /// Số sao đánh giá từ 1 đến 5 sao.
        /// </summary>
        [Range(1, 5, ErrorMessage = "Đánh giá sao phải từ 1 đến 5.")]
        public int Rating { get; set; } = 5;

        /// <summary>
        /// Nội dung nhận xét chi tiết (tối đa 500 ký tự).
        /// </summary>
        [StringLength(500, ErrorMessage = "Nhận xét tối đa 500 ký tự.")]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = VnTime.Now;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Trạng thái hiển thị (Owner hoặc Admin có thể ẩn nếu vi phạm). Mặc định hiển thị công khai.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Phản hồi của chủ phòng gym cho đánh giá này (nếu có).
        /// </summary>
        [StringLength(1000, ErrorMessage = "Phản hồi tối đa 1000 ký tự.")]
        public string? OwnerReply { get; set; }

        public DateTime? OwnerRepliedAt { get; set; }

        // Navigation properties
        [ForeignKey("GymId")]
        public virtual Gym Gym { get; set; } = null!;

        [ForeignKey("MemberId")]
        public virtual ApplicationUser Member { get; set; } = null!;
    }
}
