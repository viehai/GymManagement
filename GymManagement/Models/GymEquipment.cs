using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
    public class GymEquipment
    {
        public int Id { get; set; }

        [Required]
        public int GymId { get; set; }

        // NULL nếu IsCustom = true (Owner tự thêm máy không có trong catalog)
        public int? EquipmentId { get; set; }

        public bool IsVisible { get; set; } = true;

        public bool IsCustom { get; set; } = false;

        [StringLength(150)]
        public string CustomName { get; set; } = string.Empty;

        [StringLength(500)]
        public string CustomImage { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("GymId")]
        [ValidateNever]
        public Gym? Gym { get; set; }

        [ForeignKey("EquipmentId")]
        [ValidateNever]
        public Equipment? Equipment { get; set; }
    }
}