using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

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

        /// <summary>
        /// Sức chứa tối đa của phòng gym cùng lúc (phục vụ tính năng Crowd Meter / Heatmap).
        /// </summary>
        [Range(5, 2000, ErrorMessage = "Sức chứa phòng gym từ 5 đến 2000 người.")]
        public int MaxCapacity { get; set; } = 50;

        public DateTime CreatedAt { get; set; } = VnTime.Now;

        // Navigation properties
        [ForeignKey("OwnerId")]
        public ApplicationUser Owner { get; set; }

        public ICollection<GymEquipment> GymEquipments { get; set; }
        public ICollection<MembershipPackage> MembershipPackages { get; set; }
        public ICollection<MemberMembership> MemberMemberships { get; set; }
        public ICollection<GymImage> GymImages { get; set; }
        public ICollection<MemberSuspension> MemberSuspensions { get; set; } = new List<MemberSuspension>();
        public ICollection<VipTierSetting> VipTierSettings { get; set; } = new List<VipTierSetting>();
        public ICollection<MemberVipStatus> MemberVipStatuses { get; set; } = new List<MemberVipStatus>();
        public ICollection<CheckinLog> CheckinLogs { get; set; } = new List<CheckinLog>();
        public ICollection<GymReview> GymReviews { get; set; } = new List<GymReview>();
    }
}