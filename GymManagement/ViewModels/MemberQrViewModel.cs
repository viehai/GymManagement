using System;
using System.Collections.Generic;

namespace GymManagement.ViewModels
{
    public class MemberQrViewModel
    {
        public string MemberId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public string QrPayload { get; set; } = string.Empty;

        public string VipTierName { get; set; } = "Standard";
        public string VipBadgeColor { get; set; } = "#64748b";

        public List<ActiveMemberCardItem> ActiveMemberships { get; set; } = new();
    }

    public class ActiveMemberCardItem
    {
        public int MembershipId { get; set; }
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsExpiringSoon => DaysRemaining <= 7 && DaysRemaining >= 0;
        public string PackageQrPayload { get; set; } = string.Empty;
    }
}
