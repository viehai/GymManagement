using System;
using System.Linq;
using System.Threading.Tasks;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminTransactionController : Controller
    {
        private readonly GymDbContext _context;

        public AdminTransactionController(GymDbContext context)
        {
            _context = context;
        }

        // ==================== ADM-16: DANH SÁCH GIAO DỊCH TOÀN HỆ THỐNG ====================
        // GET: /AdminTransaction/Index?gymId=...&status=all&search=...
        public async Task<IActionResult> Index(int? gymId = null, string? status = "all", string? search = null)
        {
            // 1. Lấy toàn bộ danh sách phòng Gym đã duyệt để làm bộ lọc
            var availableGyms = await _context.Gyms
                .Where(g => g.Status == "Approved" || g.Status == "Suspended")
                .OrderBy(g => g.Name)
                .Select(g => new GymDropdownItem
                {
                    Id = g.Id,
                    Name = g.Name
                })
                .ToListAsync();

            // 2. Lấy toàn bộ giao dịch từ CSDL kèm thông tin liên quan
            var rawTransactions = await _context.Transactions
                .Include(t => t.Member)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Gym)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Package)
                .Include(t => t.Invoice)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // 3. Tính toán KPI Toàn sàn
            decimal totalRevenue = rawTransactions
                .Where(t => string.Equals(t.Status, "Success", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            int totalTransactions = rawTransactions.Count;
            int successCount = rawTransactions.Count(t => string.Equals(t.Status, "Success", StringComparison.OrdinalIgnoreCase));
            int pendingCount = rawTransactions.Count(t => string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            int failedCount = rawTransactions.Count(t => string.Equals(t.Status, "Failed", StringComparison.OrdinalIgnoreCase));

            // 4. Áp dụng bộ lọc
            var query = rawTransactions.AsEnumerable();

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(t => t.Membership?.GymId == gymId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = query.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(t =>
                    (t.Member != null && t.Member.FullName != null && t.Member.FullName.ToLower().Contains(kw)) ||
                    (t.Member != null && t.Member.Email != null && t.Member.Email.ToLower().Contains(kw)) ||
                    (t.Invoice != null && t.Invoice.InvoiceCode != null && t.Invoice.InvoiceCode.ToLower().Contains(kw)) ||
                    (t.VnpTxnRef != null && t.VnpTxnRef.ToLower().Contains(kw)) ||
                    (t.Membership?.Gym != null && t.Membership.Gym.Name != null && t.Membership.Gym.Name.ToLower().Contains(kw)) ||
                    (t.Membership?.Package != null && t.Membership.Package.Name != null && t.Membership.Package.Name.ToLower().Contains(kw))
                );
            }

            var items = query.Select(t => new AdminTransactionItemViewModel
            {
                Id = t.Id,
                MemberId = t.MemberId,
                MemberFullName = t.Member?.FullName ?? "Hội viên ẩn",
                MemberEmail = t.Member?.Email ?? string.Empty,
                GymId = t.Membership?.GymId ?? 0,
                GymName = t.Membership?.Gym?.Name ?? "—",
                GymAddress = t.Membership?.Gym?.Address ?? "—",
                PackageName = t.Membership?.Package?.Name ?? "—",
                PackageType = t.Membership?.Package?.PackageType ?? "Daily",
                DurationInMonths = t.Membership?.Package?.DurationInMonths,
                Amount = t.Amount,
                Status = char.ToUpper(t.Status[0]) + t.Status.Substring(1).ToLower(),
                PaymentMethod = t.PaymentMethod ?? "VNPay",
                VnpTxnRef = t.VnpTxnRef,
                InvoiceCode = t.Invoice?.InvoiceCode,
                InvoiceId = t.Invoice?.Id,
                CreatedAt = t.CreatedAt
            }).ToList();

            var vm = new AdminTransactionListViewModel
            {
                Transactions = items,
                TotalRevenue = totalRevenue,
                TotalTransactions = totalTransactions,
                SuccessCount = successCount,
                PendingCount = pendingCount,
                FailedCount = failedCount,
                SelectedGymId = gymId,
                SelectedStatus = status ?? "all",
                SearchKeyword = search,
                AvailableGyms = availableGyms
            };

            return View(vm);
        }
    }
}
