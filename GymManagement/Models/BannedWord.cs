using System.ComponentModel.DataAnnotations;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    /// <summary>
    /// Danh sách từ ngữ cấm xuất hiện trong nhận xét, đánh giá (do Admin thiết lập).
    /// </summary>
    public class BannedWord
    {
        public int Id { get; set; }

        /// <summary>
        /// Từ ngữ hoặc cụm từ bị cấm (lưu dạng chữ thường, không phân biệt hoa thường khi kiểm tra).
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập từ ngữ cấm.")]
        [StringLength(100, ErrorMessage = "Từ ngữ tối đa 100 ký tự.")]
        public string Word { get; set; } = string.Empty;

        /// <summary>
        /// Phân loại từ cấm (VD: Thô tục, Xúc phạm, Lừa đảo, Quảng cáo/Spam).
        /// </summary>
        [StringLength(50)]
        public string? Category { get; set; } = "Thô tục";

        public DateTime CreatedAt { get; set; } = VnTime.Now;
    }
}
