using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GymManagement.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thiết bị / máy tập.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(500)]
        [ValidateNever]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phân loại nhóm cơ.")]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        // Navigation property
        [ValidateNever]
        public ICollection<GymEquipment>? GymEquipments { get; set; }
    }
}