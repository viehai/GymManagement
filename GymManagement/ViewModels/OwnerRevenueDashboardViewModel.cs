namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Báo cáo doanh thu & Dashboard của Owner (OwnerDashboard/Revenue).
    /// </summary>
    public class OwnerRevenueDashboardViewModel
    {
        public int? SelectedGymId { get; set; }
        public List<GymManagement.Models.Gym> MyGyms { get; set; } = new();

        // ── Thống kê tổng hợp ──
        public decimal TotalRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal LastMonthRevenue { get; set; }
        public int TotalActiveMembers { get; set; }
        public int TotalSuccessfulTransactions { get; set; }

        // ── Biểu đồ doanh thu 6 tháng gần nhất ──
        public List<MonthlyRevenueItem> MonthlyRevenueChart { get; set; } = new();

        // ── Top các gói bán chạy nhất ──
        public List<TopPackageRevenueItem> TopPackages { get; set; } = new();

        // ── Giao dịch gần nhất ──
        public List<OwnerTransactionItemViewModel> RecentTransactions { get; set; } = new();
    }

    public class MonthlyRevenueItem
    {
        public string MonthLabel { get; set; } = string.Empty; // "Tháng 03/2026"
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
}
