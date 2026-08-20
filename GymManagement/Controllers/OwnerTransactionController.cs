using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Quản lý danh sách các giao dịch thanh toán tại các cơ sở phòng gym của Owner (OWN-17).
    /// </summary>
    [Authorize(Roles = "Owner")]
    public class OwnerTransactionController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerTransactionController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id ?? string.Empty;
        }

        // ==================== OWN-17: DANH SÁCH GIAO DỊCH ====================
        // GET /OwnerTransaction/Index?gymId=...&status=...&fromDate=...&toDate=...
        public async Task<IActionResult> Index(int? gymId, string? status, DateTime? fromDate, DateTime? toDate)
        {
            var userId = await GetCurrentUserIdAsync();

            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderBy(g => g.Name)
                .ToListAsync();

            var query = _context.Transactions
                .Include(t => t.Member)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Gym)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Package)
                .Include(t => t.Invoice)
                .Where(t => t.Membership != null && t.Membership.Gym.OwnerId == userId);

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(t => t.Membership.GymId == gymId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.CreatedAt <= endOfDay);
            }

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var vm = new OwnerTransactionListViewModel
            {
                SelectedGymId  = gymId,
                SelectedStatus = status,
                FromDate       = fromDate,
                ToDate         = toDate,
                MyGyms         = myGyms,
                Transactions   = transactions.Select(t => new OwnerTransactionItemViewModel
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
                }).ToList()
            };

            return View(vm);
        }
    }
}
