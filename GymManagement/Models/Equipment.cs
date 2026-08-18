using System.ComponentModel.DataAnnotations;

namespace GymManagement.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        public string Description { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        [StringLength(100)]
        public string Category { get; set; }

        // Navigation property
        public ICollection<GymEquipment> GymEquipments { get; set; }
    }
}