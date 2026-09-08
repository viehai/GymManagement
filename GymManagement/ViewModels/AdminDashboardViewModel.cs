using GymManagement.Models;

namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel chuyên dụng cho Dashboard Quản trị viên (Admin/Dashboard).
    /// </summary>
    public class AdminDashboardViewModel
    {
        // ── Tài chính & Giao dịch sàn ──
        public decimal TotalGMV { get; set; }
        public decimal ThisMonthGMV { get; set; }
        public decimal LastMonthGMV { get; set; }
        public double GrowthRateMoM { get; set; }
        public int TotalTransactions { get; set; }

        // ── Phòng Gym ──
        public int TotalGyms { get; set; }
        public int ApprovedGyms { get; set; }
        public int PendingGyms { get; set; }
        public int RejectedGyms { get; set; }

        // ── Người dùng ──
        public int TotalUsers { get; set; }
        public int TotalMembers { get; set; }
        public int TotalOwners { get; set; }

        // ── VIP Loyalty toàn sàn ──
        public int TotalVipMembers { get; set; }
        public int SilverVipCount { get; set; }
        public int GoldVipCount { get; set; }
        public int PlatinumVipCount { get; set; }

        // ── Kỷ luật & Đình chỉ toàn sàn ──
        public int TotalActiveSuspensions { get; set; }
        public int TotalLiftedSuspensions { get; set; }

        // ── Biểu đồ Doanh thu toàn sàn 6 tháng ──
        public List<MonthlyRevenueItem> MonthlyRevenueChart { get; set; } = new();

        // ── Top Phòng Gym doanh thu cao nhất ──
        public List<AdminTopGymItemViewModel> TopGymsByRevenue { get; set; } = new();

        // ── Phòng Gym có nhiều ca đình chỉ nhất (Cảnh báo rủi ro) ──
        public List<AdminGymSuspensionItemViewModel> TopGymsBySuspensions { get; set; } = new();

        // ── Cơ sở Gym đăng ký gần nhất ──
        public List<Gym> RecentGyms { get; set; } = new();
    }

    public class AdminTopGymItemViewModel
    {
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalMembers { get; set; }
        public int TotalPackages { get; set; }
    }

    public class AdminGymSuspensionItemViewModel
    {
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public int ActiveSuspensionsCount { get; set; }
        public int TotalSuspensionsCount { get; set; }
    }
}
