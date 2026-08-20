using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Báo cáo doanh thu, phân tích gói bán chạy và biểu đồ tăng trưởng của Owner (OWN-18).
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

        // ==================== OWN-18: BÁO CÁO DOANH THU ====================
        // GET /OwnerDashboard/Revenue?gymId=...
        public async Task<IActionResult> Revenue(int? gymId)
        {
            var userId = await GetCurrentUserIdAsync();

            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderBy(g => g.Name)
                .ToListAsync();

            // Lọc giao dịch thành công
            var txQuery = _context.Transactions
                .Include(t => t.Member)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Gym)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Package)
                .Include(t => t.Invoice)
                .Where(t => t.Status == "Success" && t.Membership != null && t.Membership.Gym.OwnerId == userId);

            // Lọc membership active
            var memberQuery = _context.MemberMemberships
                .Include(m => m.Gym)
                .Where(m => m.Gym.OwnerId == userId && m.EndDate >= DateTime.Today);

            if (gymId.HasValue && gymId.Value > 0)
            {
                txQuery = txQuery.Where(t => t.Membership.GymId == gymId.Value);
                memberQuery = memberQuery.Where(m => m.GymId == gymId.Value);
            }

            var allTransactions = await txQuery.ToListAsync();
            var activeMemberships = await memberQuery.ToListAsync();

            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);
            var endOfLastMonth   = startOfThisMonth.AddTicks(-1);

            // ── Thống kê 6 tháng gần nhất cho biểu đồ ──
            var monthlyChart = new List<MonthlyRevenueItem>();
            for (int i = 5; i >= 0; i--)
            {
                var targetMonthStart = startOfThisMonth.AddMonths(-i);
                var targetMonthEnd   = targetMonthStart.AddMonths(1).AddTicks(-1);

                var monthTxs = allTransactions
                    .Where(t => t.CreatedAt >= targetMonthStart && t.CreatedAt <= targetMonthEnd)
                    .ToList();

                monthlyChart.Add(new MonthlyRevenueItem
                {
                    MonthLabel       = $"T{targetMonthStart.Month}/{targetMonthStart.Year}",
                    Revenue          = monthTxs.Sum(t => t.Amount),
                    TransactionCount = monthTxs.Count
                });
            }

            // ── Top các gói bán chạy nhất ──
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

            // ── Giao dịch gần nhất ──
            var recentTxs = allTransactions
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new OwnerTransactionItemViewModel
                {
                    TransactionId = t.Id,
                    MemberName    = t.Member?.FullName ?? "—",
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
                MyGyms                       = myGyms,
                TotalRevenue                 = allTransactions.Sum(t => t.Amount),
                ThisMonthRevenue             = allTransactions.Where(t => t.CreatedAt >= startOfThisMonth).Sum(t => t.Amount),
                LastMonthRevenue             = allTransactions.Where(t => t.CreatedAt >= startOfLastMonth && t.CreatedAt <= endOfLastMonth).Sum(t => t.Amount),
                TotalActiveMembers           = activeMemberships.Select(m => m.MemberId).Distinct().Count(),
                TotalSuccessfulTransactions  = allTransactions.Count,
                MonthlyRevenueChart          = monthlyChart,
                TopPackages                  = topPackages,
                RecentTransactions           = recentTxs
            };

            return View(vm);
        }
    }
}
