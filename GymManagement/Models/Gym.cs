using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
    public class Gym
    {
        public int Id { get; set; }

        [Required]
        public string OwnerId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(300)]
        public string Address { get; set; }

        public string Description { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        // Pending / Approved / Rejected / Suspended
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("OwnerId")]
        public ApplicationUser Owner { get; set; }

        public ICollection<GymEquipment> GymEquipments { get; set; }
        public ICollection<MembershipPackage> MembershipPackages { get; set; }
        public ICollection<MemberMembership> MemberMemberships { get; set; }
    }
}