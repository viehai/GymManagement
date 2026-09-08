using GymManagement.Models;

namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Báo cáo doanh thu & Dashboard của Owner (OwnerDashboard/Revenue).
    /// </summary>
    public class OwnerRevenueDashboardViewModel
    {
        public int? SelectedGymId { get; set; }
        public string SelectedPeriod { get; set; } = "this_month";
        public string PeriodLabel { get; set; } = "Tháng này";
        public List<Gym> MyGyms { get; set; } = new();

        // ── Thống kê chỉ số chính (KPIs) ──
        public decimal TotalRevenue { get; set; }
        public decimal PeriodRevenue { get; set; }
        public decimal PreviousPeriodRevenue { get; set; }
        public double GrowthRateMoM { get; set; }
        public int TotalActiveMembers { get; set; }
        public int ExpiringSoonMembersCount { get; set; }
        public double RenewalRatePercent { get; set; }
        public int TotalSuccessfulTransactions { get; set; }
        public int TotalVipMembers { get; set; }

        // ── Phân bổ Hạng VIP (Donut Chart) ──
        public List<VipTierDistributionItem> VipDistribution { get; set; } = new();

        // ── Phân bổ loại gói dịch vụ (Donut/Pie Chart) ──
        public List<PackageTypeDistributionItem> PackageTypeDistribution { get; set; } = new();

        // ── Biểu đồ doanh thu theo các tháng gần nhất (Bar & Line Chart) ──
        public List<MonthlyRevenueItem> MonthlyRevenueChart { get; set; } = new();

        // ── Top các gói bán chạy nhất ──
        public List<TopPackageRevenueItem> TopPackages { get; set; } = new();

        // ── Top hội viên chi tiêu nhiều nhất ──
        public List<TopSpenderItemViewModel> TopSpenders { get; set; } = new();

        // ── Giao dịch gần nhất ──
        public List<OwnerTransactionItemViewModel> RecentTransactions { get; set; } = new();
    }

    public class MonthlyRevenueItem
    {
        public string MonthLabel { get; set; } = string.Empty; // "T10/2025"
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
    }

    public class TopPackageRevenueItem
    {
        public string PackageName { get; set; } = string.Empty;
        public string GymName { get; set; } = string.Empty;
        public string PackageTypeLabel { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class VipTierDistributionItem
    {
        public string TierName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public double Percentage { get; set; }
        public string ColorHex { get; set; } = "#6366f1";
    }

    public class PackageTypeDistributionItem
    {
        public string TypeName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TotalSold { get; set; }
        public double Percentage { get; set; }
        public string ColorHex { get; set; } = "#10b981";
    }

    public class TopSpenderItemViewModel
    {
        public string MemberId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string GymName { get; set; } = string.Empty;
        public string VipTier { get; set; } = "Standard";
        public decimal TotalSpent { get; set; }
        public int PurchaseCount { get; set; }
    }
}
