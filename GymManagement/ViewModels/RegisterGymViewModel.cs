using System.ComponentModel.DataAnnotations;

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

        [Display(Name = "Hình ảnh đại diện")]
        public IFormFile? ImageFile { get; set; }
    }
}
