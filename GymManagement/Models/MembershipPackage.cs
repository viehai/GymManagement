using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
    public class MembershipPackage
    {
        public int Id { get; set; }

        [Required]
        public int GymId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        // "Daily" hoặc "Monthly"
        [Required]
        [StringLength(20)]
        public string PackageType { get; set; }

        // NULL nếu PackageType = "Daily"
        public int? DurationInMonths { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("GymId")]
        public Gym Gym { get; set; }

        public ICollection<MemberMembership> MemberMemberships { get; set; }
    }
}