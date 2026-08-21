using System.ComponentModel.DataAnnotations;

namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho form tự thêm thiết bị Custom vào phòng Gym (OWN-08).
    /// </summary>
    public class OwnerEquipmentCustomViewModel
    {
        public int GymId { get; set; }
        public string? GymName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thiết bị tự chọn.")]
        [StringLength(150, ErrorMessage = "Tên thiết bị không vượt quá 150 ký tự.")]
        [Display(Name = "Tên thiết bị / máy tập")]
        public string CustomName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phân loại nhóm cơ.")]
        [StringLength(100, ErrorMessage = "Phân loại không vượt quá 100 ký tự.")]
        [Display(Name = "Phân loại nhóm cơ")]
        public string CustomCategory { get; set; } = string.Empty;

        public List<string> Categories { get; set; } = new();
    }
}
