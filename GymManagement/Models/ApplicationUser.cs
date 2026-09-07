using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GymManagement.Models
{
    // Kế thừa IdentityUser để có sẵn Email, PasswordHash, PhoneNumber, EmailConfirmed...
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(150)]
        public string FullName { get; set; }

        // Navigation properties
        public ICollection<Gym> Gyms { get; set; }
        public ICollection<MemberMembership> MemberMemberships { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
        public ICollection<MemberSuspension> Suspensions { get; set; } = new List<MemberSuspension>();
        public ICollection<MemberSuspension> ExecutedSuspensions { get; set; } = new List<MemberSuspension>();
    }
}