using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Báo cáo doanh thu, phân tích chỉ số kinh doanh, VIP Loyalty và xuất Excel của Chủ phòng Gym (Module 7).
    /// </summary>
    [Authorize(Roles = "Owner")]
    public class OwnerDashboardController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerDashboardController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id ?? string.Empty;
        }

        // ==================== BÁO CÁO DOANH THU & DASHBOARD ====================
        // GET /OwnerDashboard/Revenue?gymId=...&period=...
        public async Task<IActionResult> Revenue(int? gymId, string period = "this_month")
        {
            var userId = await GetCurrentUserIdAsync();

            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderBy(g => g.Name)
                .ToListAsync();

            // 1. Xác định mốc thời gian theo bộ lọc
            var (periodStart, periodEnd, prevStart, prevEnd, periodLabel) = GetDateRangeForPeriod(period);

            // 2. Lọc giao dịch thành công của Owner
            var txQuery = _context.Transactions
                .Include(t => t.Member)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Gym)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Package)
                .Include(t => t.Invoice)
                .Where(t => t.Status == "Success" && t.Membership != null && t.Membership.Gym.OwnerId == userId);

            // 3. Lọc Membership của Owner
            var memberQuery = _context.MemberMemberships
                .Include(m => m.Gym)
                .Include(m => m.Member)
                .Include(m => m.Package)
                .Where(m => m.Gym.OwnerId == userId);

            // 4. Lọc VIP Status của Owner
            var vipQuery = _context.MemberVipStatuses
                .Include(v => v.Gym)
                .Include(v => v.Member)
                .Include(v => v.CurrentTier)
                .Where(v => v.Gym.OwnerId == userId);

            // Áp dụng lọc theo từng cơ sở nếu có
            if (gymId.HasValue && gymId.Value > 0)
            {
                txQuery = txQuery.Where(t => t.Membership.GymId == gymId.Value);
                memberQuery = memberQuery.Where(m => m.GymId == gymId.Value);
                vipQuery = vipQuery.Where(v => v.GymId == gymId.Value);
            }

            var allTransactions = await txQuery.ToListAsync();
            var allMemberships  = await memberQuery.ToListAsync();
            var allVipStatuses   = await vipQuery.ToListAsync();

            // Tính toán doanh thu kỳ này vs kỳ trước
            var periodTransactions = allTransactions
                .Where(t => t.CreatedAt >= periodStart && t.CreatedAt <= periodEnd)
                .ToList();

            var prevTransactions = allTransactions
                .Where(t => t.CreatedAt >= prevStart && t.CreatedAt <= prevEnd)
                .ToList();

            decimal totalRevenue = allTransactions.Sum(t => t.Amount);
            decimal periodRevenue = periodTransactions.Sum(t => t.Amount);
            decimal prevRevenue = prevTransactions.Sum(t => t.Amount);

            double growthMoM = 0.0;
            if (prevRevenue > 0)
            {
                growthMoM = (double)((periodRevenue - prevRevenue) / prevRevenue) * 100.0;
            }
            else if (periodRevenue > 0)
            {
                growthMoM = 100.0;
            }

            // Hội viên đang tập (active) và sắp hết hạn (<= 7 ngày)
            var today = VnTime.Today;
            var activeMemberships = allMemberships.Where(m => m.EndDate >= today).ToList();
            int activeMembersCount = activeMemberships.Select(m => m.MemberId).Distinct().Count();

            var next7Days = today.AddDays(7);
            int expiringSoonCount = activeMemberships
                .Where(m => m.EndDate >= today && m.EndDate <= next7Days)
                .Select(m => m.MemberId)
                .Distinct()
                .Count();

            // Tính tỷ lệ gia hạn (% Renewal Rate)
            var expiredMemberIds = allMemberships
                .Where(m => m.EndDate < today)
                .Select(m => m.MemberId)
                .Distinct()
                .ToList();

            int renewedCount = 0;
            if (expiredMemberIds.Any())
            {
                var activeMemberIds = activeMemberships.Select(m => m.MemberId).Distinct().ToHashSet();
                renewedCount = expiredMemberIds.Count(id => activeMemberIds.Contains(id));
            }
            double renewalRate = expiredMemberIds.Count > 0 ? ((double)renewedCount / expiredMemberIds.Count) * 100.0 : 0.0;

            // ── Thống kê 6 tháng gần nhất cho biểu đồ doanh thu (Chart.js) ──
            var now = VnTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var monthlyChart = new List<MonthlyRevenueItem>();
            for (int i = 5; i >= 0; i--)
            {
                var mStart = startOfThisMonth.AddMonths(-i);
                var mEnd   = mStart.AddMonths(1).AddTicks(-1);

                var monthTxs = allTransactions
                    .Where(t => t.CreatedAt >= mStart && t.CreatedAt <= mEnd)
                    .ToList();

                monthlyChart.Add(new MonthlyRevenueItem
                {
                    MonthLabel       = $"T{mStart.Month}/{mStart.Year}",
                    Revenue          = monthTxs.Sum(t => t.Amount),
                    TransactionCount = monthTxs.Count
                });
            }

            // ── Phân bổ Hạng VIP (Donut Chart) ──
            int silverCount = allVipStatuses.Count(v => v.CurrentTier?.TierName?.ToLower().Contains("silver") == true || v.CurrentTier?.TierName?.ToLower().Contains("bạc") == true);
            int goldCount = allVipStatuses.Count(v => v.CurrentTier?.TierName?.ToLower().Contains("gold") == true || v.CurrentTier?.TierName?.ToLower().Contains("vàng") == true);
            int platinumCount = allVipStatuses.Count(v => v.CurrentTier?.TierName?.ToLower().Contains("platinum") == true || v.CurrentTier?.TierName?.ToLower().Contains("bạch kim") == true || v.CurrentTier?.TierName?.ToLower().Contains("kim cương") == true);
            int totalVip = allVipStatuses.Count(v => v.CurrentTier != null);
            int standardCount = Math.Max(0, activeMembersCount - totalVip);

            int totalTrackedMembers = Math.Max(1, totalVip + standardCount);
            var vipDistribution = new List<VipTierDistributionItem>
            {
                new() { TierName = "Standard", MemberCount = standardCount, Percentage = Math.Round((double)standardCount / totalTrackedMembers * 100, 1), ColorHex = "#64748b" }
            };

            var vipTierGroups = allVipStatuses
                .Where(v => v.CurrentTier != null)
                .GroupBy(v => new { v.CurrentTier!.TierName, Color = !string.IsNullOrEmpty(v.CurrentTier.BadgeColor) ? v.CurrentTier.BadgeColor : "#3b82f6" });

            foreach (var tg in vipTierGroups)
            {
                vipDistribution.Add(new VipTierDistributionItem
                {
                    TierName = tg.Key.TierName,
                    MemberCount = tg.Count(),
                    Percentage = Math.Round((double)tg.Count() / totalTrackedMembers * 100, 1),
                    ColorHex = tg.Key.Color
                });
            }

            // ── Phân bổ doanh thu theo loại gói ──
            var dailyTxs = periodTransactions.Where(t => t.Membership?.Package?.PackageType == "Daily").ToList();
            var monthlyTxs = periodTransactions.Where(t => t.Membership?.Package?.PackageType != "Daily").ToList();

            decimal dailyRev = dailyTxs.Sum(t => t.Amount);
            decimal monthlyRev = monthlyTxs.Sum(t => t.Amount);
            decimal totalDistRev = Math.Max(1, dailyRev + monthlyRev);

            var pkgDistribution = new List<PackageTypeDistributionItem>
            {
                new() { TypeName = "Vé ngày (Daily Pass)", Revenue = dailyRev, TotalSold = dailyTxs.Count, Percentage = Math.Round((double)(dailyRev / totalDistRev) * 100, 1), ColorHex = "#10b981" },
                new() { TypeName = "Gói định kỳ (Tháng/Năm)", Revenue = monthlyRev, TotalSold = monthlyTxs.Count, Percentage = Math.Round((double)(monthlyRev / totalDistRev) * 100, 1), ColorHex = "#3b82f6" }
            };

            // ── Top 5 gói bán chạy nhất ──
            var topPackages = allTransactions
                .Where(t => t.Membership?.Package != null)
                .GroupBy(t => new { t.Membership.PackageId, t.Membership.Package.Name, t.Membership.Package.PackageType, t.Membership.Package.DurationInMonths, GymName = t.Membership.Gym.Name })
                .Select(g => new TopPackageRevenueItem
                {
                    PackageName      = g.Key.Name,
                    GymName          = g.Key.GymName,
                    PackageTypeLabel = g.Key.PackageType == "Daily" ? "Vé ngày" : $"Gói {g.Key.DurationInMonths} tháng",
                    TotalSold        = g.Count(),
                    TotalRevenue     = g.Sum(x => x.Amount)
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(5)
                .ToList();

            // ── Top 5 hội viên chi tiêu nhiều nhất ──
            var topSpenders = allTransactions
                .Where(t => t.Member != null)
                .GroupBy(t => new { t.MemberId, MemberName = t.Member.FullName ?? t.Member.UserName ?? "Hội viên", t.Member.Email, GymName = t.Membership?.Gym?.Name ?? "—" })
                .Select(g =>
                {
                    var vipTier = allVipStatuses.FirstOrDefault(v => v.MemberId == g.Key.MemberId)?.CurrentTier?.TierName ?? "Standard";
                    return new TopSpenderItemViewModel
                    {
                        MemberId      = g.Key.MemberId,
                        FullName      = g.Key.MemberName,
                        Email         = g.Key.Email ?? "—",
                        GymName       = g.Key.GymName,
                        VipTier       = vipTier,
                        TotalSpent    = g.Sum(x => x.Amount),
                        PurchaseCount = g.Count()
                    };
                })
                .OrderByDescending(s => s.TotalSpent)
                .Take(5)
                .ToList();

            // ── Giao dịch gần nhất ──
            var recentTxs = allTransactions
                .OrderByDescending(t => t.CreatedAt)
                .Take(6)
                .Select(t => new OwnerTransactionItemViewModel
                {
                    TransactionId = t.Id,
                    MemberName    = t.Member?.FullName ?? t.Member?.UserName ?? "—",
                    MemberEmail   = t.Member?.Email ?? "—",
                    GymName       = t.Membership?.Gym?.Name ?? "—",
                    PackageName   = t.Membership?.Package?.Name ?? "—",
                    Amount        = t.Amount,
                    Status        = t.Status,
                    PaymentMethod = t.PaymentMethod,
                    VnpTxnRef     = t.VnpTxnRef,
                    CreatedAt     = t.CreatedAt,
                    InvoiceCode   = t.Invoice?.InvoiceCode
                })
                .ToList();

            var vm = new OwnerRevenueDashboardViewModel
            {
                SelectedGymId                = gymId,
                SelectedPeriod               = period,
                PeriodLabel                  = periodLabel,
                MyGyms                       = myGyms,
                TotalRevenue                 = totalRevenue,
                PeriodRevenue                = periodRevenue,
                PreviousPeriodRevenue        = prevRevenue,
                GrowthRateMoM                = Math.Round(growthMoM, 1),
                TotalActiveMembers           = activeMembersCount,
                ExpiringSoonMembersCount     = expiringSoonCount,
                RenewalRatePercent           = Math.Round(renewalRate, 1),
                TotalSuccessfulTransactions  = periodTransactions.Count,
                TotalVipMembers              = totalVip,
                VipDistribution              = vipDistribution,
                PackageTypeDistribution      = pkgDistribution,
                MonthlyRevenueChart          = monthlyChart,
                TopPackages                  = topPackages,
                TopSpenders                  = topSpenders,
                RecentTransactions           = recentTxs
            };

            return View(vm);
        }

        // ==================== XUẤT EXCEL BÁO CÁO DOANH THU ====================
        // GET /OwnerDashboard/ExportRevenueExcel?gymId=...&period=...
        public async Task<IActionResult> ExportRevenueExcel(int? gymId, string period = "this_month")
        {
            var userId = await GetCurrentUserIdAsync();

            var gym = gymId.HasValue && gymId.Value > 0
                ? await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId.Value && g.OwnerId == userId)
                : null;

            string gymName = gym?.Name ?? "Tất cả cơ sở";
            var (periodStart, periodEnd, prevStart, prevEnd, periodLabel) = GetDateRangeForPeriod(period);

            var txQuery = _context.Transactions
                .Include(t => t.Member)
                .Include(t => t.Membership).ThenInclude(m => m.Gym)
                .Include(t => t.Membership).ThenInclude(m => m.Package)
                .Where(t => t.Status == "Success" && t.Membership != null && t.Membership.Gym.OwnerId == userId);

            var memberQuery = _context.MemberMemberships
                .Include(m => m.Gym)
                .Include(m => m.Member)
                .Include(m => m.Package)
                .Where(m => m.Gym.OwnerId == userId);

            var vipQuery = _context.MemberVipStatuses
                .Include(v => v.Gym)
                .Include(v => v.CurrentTier)
                .Where(v => v.Gym.OwnerId == userId);

            if (gymId.HasValue && gymId.Value > 0)
            {
                txQuery = txQuery.Where(t => t.Membership.GymId == gymId.Value);
                memberQuery = memberQuery.Where(m => m.GymId == gymId.Value);
                vipQuery = vipQuery.Where(v => v.GymId == gymId.Value);
            }

            var allTransactions = await txQuery.ToListAsync();
            var allMemberships  = await memberQuery.ToListAsync();
            var allVipStatuses   = await vipQuery.ToListAsync();

            var periodTransactions = allTransactions
                .Where(t => t.CreatedAt >= periodStart && t.CreatedAt <= periodEnd)
                .ToList();

            var prevTransactions = allTransactions
                .Where(t => t.CreatedAt >= prevStart && t.CreatedAt <= prevEnd)
                .ToList();

            decimal totalRevenue = allTransactions.Sum(t => t.Amount);
            decimal periodRevenue = periodTransactions.Sum(t => t.Amount);
            decimal prevRevenue = prevTransactions.Sum(t => t.Amount);
            double growthMoM = prevRevenue > 0 ? (double)((periodRevenue - prevRevenue) / prevRevenue) * 100.0 : (periodRevenue > 0 ? 100.0 : 0.0);

            var today = VnTime.Today;
            var activeMemberships = allMemberships.Where(m => m.EndDate >= today).ToList();
            int activeMembersCount = activeMemberships.Select(m => m.MemberId).Distinct().Count();

            var expiredMemberIds = allMemberships.Where(m => m.EndDate < today).Select(m => m.MemberId).Distinct().ToList();
            int renewedCount = expiredMemberIds.Count(id => activeMemberships.Any(m => m.MemberId == id));
            double renewalRate = expiredMemberIds.Count > 0 ? ((double)renewedCount / expiredMemberIds.Count) * 100.0 : 0.0;

            int silverCount = allVipStatuses.Count(v => v.CurrentTier?.TierName?.ToLower().Contains("silver") == true || v.CurrentTier?.TierName?.ToLower().Contains("bạc") == true);
            int goldCount = allVipStatuses.Count(v => v.CurrentTier?.TierName?.ToLower().Contains("gold") == true || v.CurrentTier?.TierName?.ToLower().Contains("vàng") == true);
            int platinumCount = allVipStatuses.Count(v => v.CurrentTier?.TierName?.ToLower().Contains("platinum") == true || v.CurrentTier?.TierName?.ToLower().Contains("bạch kim") == true || v.CurrentTier?.TierName?.ToLower().Contains("kim cương") == true);
            int totalVip = allVipStatuses.Count(v => v.CurrentTier != null);

            var exportData = new OwnerRevenueExportData
            {
                GymName                     = gymName,
                PeriodLabel                 = periodLabel,
                TotalRevenue                = totalRevenue,
                PeriodRevenue               = periodRevenue,
                PreviousPeriodRevenue       = prevRevenue,
                GrowthRatePercent           = Math.Round(growthMoM, 1),
                TotalSuccessfulTransactions = periodTransactions.Count,
                TotalActiveMembers          = activeMembersCount,
                RenewalRatePercent          = Math.Round(renewalRate, 1),
                TotalVipMembers             = totalVip,
                SilverCount                 = silverCount,
                GoldCount                   = goldCount,
                PlatinumCount               = platinumCount,
                Transactions = periodTransactions.Select(t => new OwnerTransactionExportRow
                {
                    Id            = t.Id,
                    MemberName    = t.Member?.FullName ?? t.Member?.UserName ?? "—",
                    MemberEmail   = t.Member?.Email ?? "—",
                    GymName       = t.Membership?.Gym?.Name ?? "—",
                    PackageName   = t.Membership?.Package?.Name ?? "—",
                    PackageType   = t.Membership?.Package?.PackageType ?? "—",
                    Amount        = t.Amount,
                    PaymentMethod = t.PaymentMethod ?? "—",
                    Status        = t.Status,
                    CreatedAt     = t.CreatedAt
                }).ToList(),
                Members = activeMemberships.Select(m =>
                {
                    var vip = allVipStatuses.FirstOrDefault(v => v.MemberId == m.MemberId);
                    var totalSpent = allTransactions.Where(t => t.MemberId == m.MemberId).Sum(t => t.Amount);
                    return new OwnerMemberExportRow
                    {
                        FullName       = m.Member?.FullName ?? m.Member?.UserName ?? "—",
                        Email          = m.Member?.Email ?? "—",
                        PhoneNumber    = m.Member?.PhoneNumber ?? "—",
                        GymName        = m.Gym?.Name ?? "—",
                        CurrentPackage = m.Package?.Name ?? "—",
                        StartDate      = m.StartDate,
                        EndDate        = m.EndDate,
                        Status         = m.EndDate >= today ? "Đang tập" : "Đã hết hạn",
                        VipTier        = vip?.CurrentTier?.TierName ?? "Standard",
                        TotalSpent     = totalSpent
                    };
                }).ToList()
            };

            byte[] fileBytes = ExcelExportHelper.ExportOwnerRevenueReport(exportData);
            string safeGymSlug = string.Join("_", gymName.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"BaoCaoDoanhThu_{safeGymSlug}_{period}_{VnTime.Now:yyyyMMdd}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ==================== HELPER TÍNH TOÁN DATE RANGE ====================
        private static (DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, string label) GetDateRangeForPeriod(string period)
        {
            var now = VnTime.Now;
            DateTime start, end, prevStart, prevEnd;
            string label;

            switch (period?.ToLower())
            {
                case "last_month":
                    start = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    end = new DateTime(now.Year, now.Month, 1).AddTicks(-1);
                    prevStart = start.AddMonths(-1);
                    prevEnd = start.AddTicks(-1);
                    label = $"Tháng trước (T{start.Month}/{start.Year})";
                    break;

                case "this_quarter":
                    int quarter = (now.Month - 1) / 3 + 1;
                    start = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
                    end = start.AddMonths(3).AddTicks(-1);
                    prevStart = start.AddMonths(-3);
                    prevEnd = start.AddTicks(-1);
                    label = $"Quý {quarter}/{now.Year}";
                    break;

                case "this_year":
                    start = new DateTime(now.Year, 1, 1);
                    end = start.AddYears(1).AddTicks(-1);
                    prevStart = start.AddYears(-1);
                    prevEnd = start.AddTicks(-1);
                    label = $"Năm {now.Year}";
                    break;

                case "all":
                    start = DateTime.MinValue;
                    end = DateTime.MaxValue;
                    prevStart = DateTime.MinValue;
                    prevEnd = DateTime.MinValue;
                    label = "Toàn bộ thời gian";
                    break;

                case "this_month":
                default:
                    start = new DateTime(now.Year, now.Month, 1);
                    end = start.AddMonths(1).AddTicks(-1);
                    prevStart = start.AddMonths(-1);
                    prevEnd = start.AddTicks(-1);
                    label = $"Tháng này (T{now.Month}/{now.Year})";
                    break;
            }

            return (start, end, prevStart, prevEnd, label);
        }
    }
}
