using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    /// <summary>
    /// Lưu trữ vector đặc trưng khuôn mặt (FaceNet 128D Embedding) của Hội viên phục vụ Check-in / Check-out tự động (V3).
    /// </summary>
    public class MemberFaceProfile
    {
        public int Id { get; set; }

        [Required]
        public string MemberId { get; set; } = string.Empty;

        [ForeignKey("MemberId")]
        public ApplicationUser Member { get; set; } = null!;

        /// <summary>
        /// Mảng số thực 128 chiều trích xuất từ mạng nơ-ron FaceNet, được serialize dạng chuỗi JSON: [f1, f2, ..., f128].
        /// </summary>
        [Required]
        public string FaceEmbeddingJson { get; set; } = string.Empty;

        /// <summary>
        /// Đường dẫn ảnh mẫu khuôn mặt đại diện (crop 150x150) để hiển thị trực quan trên giao diện hồ sơ.
        /// </summary>
        [StringLength(255)]
        public string? SampleImageUrl { get; set; }

        /// <summary>
        /// Điểm chất lượng khuôn mặt khi chụp đăng ký (từ 0.0 đến 1.0).
        /// </summary>
        public double QualityScore { get; set; } = 1.0;

        /// <summary>
        /// Trạng thái kích hoạt Face ID (true: cho phép điểm danh bằng khuôn mặt).
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = VnTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
