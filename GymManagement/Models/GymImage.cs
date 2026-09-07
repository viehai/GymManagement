using System.ComponentModel.DataAnnotations;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    /// <summary>
    /// Lưu trữ từng ảnh trong gallery của một phòng Gym.
    /// Một Gym có thể có tối đa 10 ảnh.
    /// </summary>
    public class GymImage
    {
        public int Id { get; set; }

        [Required]
        public int GymId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>Thứ tự hiển thị — kéo thả để sắp xếp lại.</summary>
        public int DisplayOrder { get; set; }

        /// <summary>Ảnh bìa đại diện — hiển thị trên thẻ Gym ở trang Search.</summary>
        public bool IsCover { get; set; }

        public DateTime UploadedAt { get; set; } = VnTime.Now;

        // Navigation properties
        public Gym Gym { get; set; } = null!;
    }
}
