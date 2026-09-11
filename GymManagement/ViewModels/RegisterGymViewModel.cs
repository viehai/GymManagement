using System.ComponentModel.DataAnnotations;
using GymManagement.ViewModels;

namespace GymManagement.ViewModels
{
    public class RegisterGymViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên phòng Gym.")]
        [StringLength(200, ErrorMessage = "Tên không được vượt quá 200 ký tự.")]
        [Display(Name = "Tên phòng Gym")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        [StringLength(300, ErrorMessage = "Địa chỉ không được vượt quá 300 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        /// <summary>Ảnh đại diện duy nhất (legacy — giữ cho backward compat).</summary>
        [Display(Name = "Hình ảnh đại diện")]
        public IFormFile? ImageFile { get; set; }

        /// <summary>Danh sách ảnh gallery (tối đa 10). Dùng cho cả Create và Edit.</summary>
        [Display(Name = "Gallery ảnh")]
        public List<IFormFile>? GalleryFiles { get; set; }

        /// <summary>Danh sách ảnh hiện tại (dùng trong form Edit để hiển thị).</summary>
        public List<GymImageViewModel> ExistingImages { get; set; } = new();

        /// <summary>Sức chứa tối đa cùng lúc của phòng gym (phục vụ tính năng Crowd Meter).</summary>
        [Display(Name = "Sức chứa tối đa (người)")]
        [Range(5, 2000, ErrorMessage = "Sức chứa phải từ 5 đến 2000 người.")]
        public int MaxCapacity { get; set; } = 50;
    }
}
