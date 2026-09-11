using System;
using System.Collections.Generic;
using GymManagement.Models;

namespace GymManagement.ViewModels
{
    public class CheckinScanViewModel
    {
        public int SelectedGymId { get; set; }
        public string SelectedGymName { get; set; } = string.Empty;
        public List<Gym> MyGyms { get; set; } = new();

        // ── THƯỚC ĐO ĐỘ ĐÔNG ĐÚC (CROWD METER) ──
        public int MaxCapacity { get; set; } = 50;
        public int ActiveCheckinsCount { get; set; }
        public int CrowdPercentage { get; set; }
        public string CrowdStatusText { get; set; } = "Đang vắng";
        public string CrowdStatusColor { get; set; } = "#10b981";
        public string CrowdStatusIcon { get; set; } = "bi-emoji-smile";
        public string CrowdRecommendation { get; set; } = "Phòng tập đang vắng, máy tập thoáng đãng — Thời điểm lý tưởng!";

        // ── HỘI VIÊN ĐANG TRONG PHÒNG TẬP (Check-in trong 2h gần nhất) ──
        public List<LiveMemberInGymItem> ActiveMembersInGym { get; set; } = new();
    }

    public class LiveMemberInGymItem
    {
        public int CheckinId { get; set; }
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string VipTierName { get; set; } = "Standard";
        public string VipBadgeColor { get; set; } = "#64748b";
        public DateTime CheckinTime { get; set; }
        public int MinutesAgo { get; set; }
        public string CheckinMethod { get; set; } = "QrScan";
    }
}
