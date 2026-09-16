using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
    /// <summary>
    /// Lưu kết quả một buổi tập AI Pose Coach (Squat / Push-up) của hội viên.
    /// </summary>
    public class WorkoutSession
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Hội viên thực hiện buổi tập.</summary>
        [Required]
        public string MemberId { get; set; }

        /// <summary>Phòng gym hội viên trực thuộc (nullable — hỗ trợ tập không gắn gym).</summary>
        public int? GymId { get; set; }

        /// <summary>Loại bài tập: "Squat" hoặc "PushUp".</summary>
        [Required]
        [StringLength(50)]
        public string ExerciseType { get; set; }

        /// <summary>Tổng số rep đã thực hiện (kể cả rep bị lỗi form).</summary>
        public int TotalReps { get; set; }

        /// <summary>Số rep đạt chuẩn form (được tính điểm).</summary>
        public int ValidReps { get; set; }

        /// <summary>Số rep bị lỗi tư thế.</summary>
        public int InvalidReps { get; set; }

        /// <summary>Điểm chuẩn form trung bình (0 – 100 điểm).</summary>
        [Column(TypeName = "float")]
        public double AverageFormScore { get; set; }

        /// <summary>Thời gian tập luyện tính bằng giây.</summary>
        public int DurationSeconds { get; set; }

        /// <summary>Ước tính lượng calo tiêu thụ.</summary>
        [Column(TypeName = "float")]
        public double CaloriesBurned { get; set; }

        /// <summary>Thời điểm ghi nhận buổi tập (UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation Properties ─────────────────────────────────────────────
        [ForeignKey(nameof(MemberId))]
        public ApplicationUser Member { get; set; }

        [ForeignKey(nameof(GymId))]
        public Gym? Gym { get; set; }
    }
}
