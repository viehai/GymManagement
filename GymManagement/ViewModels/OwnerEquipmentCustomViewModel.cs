using GymManagement.Models;
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
    }
}
