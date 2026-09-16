using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
    /// <summary>
    /// Video mẫu chuẩn form cho từng bài tập.
    /// IsDefault = true  → Video tích hợp sẵn (Squat / Push-up), không cần GymId.
    /// IsDefault = false → Video do phòng gym tải lên riêng cho hội viên của họ.
    /// </summary>
    public class GymWorkoutVideo
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Phòng gym sở hữu (null nếu IsDefault = true).</summary>
        public int? GymId { get; set; }

        /// <summary>Loại bài tập: "Squat" hoặc "PushUp".</summary>
        [Required]
        [StringLength(50)]
        public string ExerciseType { get; set; }

        /// <summary>Tiêu đề video hiển thị.</summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        /// <summary>URL video (YouTube embed, CDN, hoặc đường dẫn nội bộ).</summary>
        [Required]
        [StringLength(500)]
        public string VideoUrl { get; set; }

        /// <summary>Ảnh thumbnail preview.</summary>
        [StringLength(500)]
        public string? ThumbnailUrl { get; set; }

        /// <summary>Mô tả ngắn về video hướng dẫn.</summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>true = video mặc định tích hợp sẵn toàn hệ thống, false = video riêng của gym.</summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>Thứ tự hiển thị (nhỏ hơn = hiển thị trước).</summary>
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation Properties ─────────────────────────────────────────────
        [ForeignKey(nameof(GymId))]
        public Gym? Gym { get; set; }
    }
}
