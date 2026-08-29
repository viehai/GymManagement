using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public string MemberId { get; set; }

        // GÃ¡n sau khi táº¡o MemberMembership thÃ nh cÃ´ng (sau khi thanh toÃ¡n OK)
        public int? MembershipId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Pending / Success / Failed
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string VnpTxnRef { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "VietQR";

        public DateTime CreatedAt { get; set; } = VnTime.Now;

        // Navigation properties
        [ForeignKey("MemberId")]
        public ApplicationUser Member { get; set; }

        [ForeignKey("MembershipId")]
        public MemberMembership Membership { get; set; }

        public Invoice Invoice { get; set; }
    }
}