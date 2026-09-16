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
        public ICollection<MemberVipStatus> MemberVipStatuses { get; set; } = new List<MemberVipStatus>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<CheckinLog> CheckinLogs { get; set; } = new List<CheckinLog>();
        public ICollection<CheckinLog> ConfirmedCheckins { get; set; } = new List<CheckinLog>();
        public ICollection<GymReview> GymReviews { get; set; } = new List<GymReview>();
        public MemberFaceProfile? FaceProfile { get; set; }
    }
}