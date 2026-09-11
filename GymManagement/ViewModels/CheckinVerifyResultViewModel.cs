using System;
using System.Collections.Generic;

namespace GymManagement.ViewModels
{
    public class CheckinVerifyResultViewModel
    {
        public bool Success { get; set; }
        public bool CanCheckin { get; set; }
        public string Message { get; set; } = string.Empty;

        public string MemberId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;

        public int? MembershipId { get; set; }
        public string? PackageName { get; set; }
        public string? PackageType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int DaysRemaining { get; set; }

        public string VipTierName { get; set; } = "Standard";
        public string VipBadgeColor { get; set; } = "#64748b";

        public bool IsSuspended { get; set; }
        public string? SuspensionReason { get; set; }

        public bool IsWrongGym { get; set; }
        public string? WrongGymName { get; set; }
        public string? TargetPackageName { get; set; }

        public bool IsExpired { get; set; }
        public bool HasActiveMembership { get; set; }

        public bool AlreadyCheckedInRecently { get; set; }
        public int? LastCheckinMinutesAgo { get; set; }

        /// <summary>
        /// Danh sách các gói tập còn hạn của hội viên tại phòng gym này
        /// (Dùng khi hội viên sở hữu từ 2 gói trở lên để lễ tân chọn)
        /// </summary>
        public List<AvailableMembershipOptionItem> AvailableMemberships { get; set; } = new();
    }

    public class AvailableMembershipOptionItem
    {
        public int MembershipId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsSelected { get; set; }
    }
}
