using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Quản lý danh sách hội viên và xem chi tiết lịch sử tập luyện của từng hội viên tại phòng gym (OWN-15, OWN-16).
    /// </summary>
    [Authorize(Roles = "Owner")]
    public class OwnerMemberController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerMemberController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id ?? string.Empty;
        }

        // ==================== OWN-15: DANH SÁCH HỘI VIÊN ====================
        // GET /OwnerMember/Index?gymId=...
        public async Task<IActionResult> Index(int? gymId)
        {
            var userId = await GetCurrentUserIdAsync();

            // Danh sách phòng Gym của Owner (đã duyệt)
            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderBy(g => g.Name)
                .ToListAsync();

            var query = _context.MemberMemberships
                .Include(m => m.Member)
                .Include(m => m.Gym)
                .Include(m => m.Package)
                .Where(m => m.Gym.OwnerId == userId);

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(m => m.GymId == gymId.Value);
            }

            var memberships = await query
                .OrderByDescending(m => m.EndDate)
                .ToListAsync();

            var vm = new OwnerMemberListViewModel
            {
                SelectedGymId = gymId,
                MyGyms = myGyms,
                Members = memberships.Select(m => new OwnerMemberItemViewModel
                {
                    MembershipId     = m.Id,
                    MemberId         = m.MemberId,
                    FullName         = m.Member?.FullName ?? "—",
                    Email            = m.Member?.Email ?? "—",
                    PhoneNumber      = m.Member?.PhoneNumber ?? "—",
                    GymId            = m.GymId,
                    GymName          = m.Gym?.Name ?? "—",
                    PackageName      = m.Package?.Name ?? "—",
                    PackageTypeLabel = m.Package?.PackageType == "Daily" ? "Vé ngày" : $"Gói {m.Package?.DurationInMonths} tháng",
                    StartDate        = m.StartDate,
                    EndDate          = m.EndDate,
                    PriceAtPurchase  = m.PriceAtPurchase,
                    PurchaseDate     = m.PurchaseDate
                }).ToList()
            };

            return View(vm);
        }

        // ==================== OWN-16: CHI TIẾT 1 HỘI VIÊN ====================
        // GET /OwnerMember/Details?memberId=...&gymId=...
        public async Task<IActionResult> Details(string memberId, int? gymId)
        {
            var userId = await GetCurrentUserIdAsync();

            var member = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == memberId);

            if (member == null) return NotFound();

            var query = _context.MemberMemberships
                .Include(m => m.Gym)
                .Include(m => m.Package)
                .Include(m => m.Transaction)
                    .ThenInclude(t => t.Invoice)
                .Where(m => m.MemberId == memberId && m.Gym.OwnerId == userId);

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(m => m.GymId == gymId.Value);
            }

            var history = await query
                .OrderByDescending(m => m.PurchaseDate)
                .ToListAsync();

            if (!history.Any()) return NotFound();

            var firstGym = history.First().Gym;

            var vm = new OwnerMemberDetailsViewModel
            {
                MemberId       = member.Id,
                FullName       = member.FullName ?? member.UserName ?? "—",
                Email          = member.Email ?? "—",
                PhoneNumber    = member.PhoneNumber ?? "—",
                GymId          = gymId ?? firstGym.Id,
                GymName        = firstGym?.Name ?? "—",
                GymAddress     = firstGym?.Address ?? "—",
                TotalSpent     = history.Sum(h => h.PriceAtPurchase),
                TotalPurchases = history.Count,
                PurchaseHistory = history.Select(h => new OwnerMemberPurchaseHistoryItem
                {
                    MembershipId     = h.Id,
                    PackageName      = h.Package?.Name ?? "—",
                    PackageTypeLabel = h.Package?.PackageType == "Daily" ? "Vé ngày" : $"Gói {h.Package?.DurationInMonths} tháng",
                    StartDate        = h.StartDate,
                    EndDate          = h.EndDate,
                    PurchaseDate     = h.PurchaseDate,
                    PriceAtPurchase  = h.PriceAtPurchase,
                    InvoiceCode      = h.Transaction?.Invoice?.InvoiceCode
                }).ToList()
            };

            return View(vm);
        }
    }
}
