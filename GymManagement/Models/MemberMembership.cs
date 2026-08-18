using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
    public class MemberMembership
    {
        public int Id { get; set; }

        [Required]
        public string MemberId { get; set; }

        [Required]
        public int GymId { get; set; }

        [Required]
        public int PackageId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtPurchase { get; set; }

        // Navigation properties
        [ForeignKey("MemberId")]
        public ApplicationUser Member { get; set; }

        [ForeignKey("GymId")]
        public Gym Gym { get; set; }

        [ForeignKey("PackageId")]
        public MembershipPackage Package { get; set; }

        public Transaction Transaction { get; set; }
    }
}