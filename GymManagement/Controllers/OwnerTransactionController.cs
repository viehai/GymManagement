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

            var myGymIds = myGyms.Select(g => g.Id).ToList();

            // Lấy tất cả giao dịch
            var rawTransactions = await _context.Transactions
                .Include(t => t.Member)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Gym)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Package)
                .Include(t => t.Invoice)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Load trước danh sách Package và Gym để map nhanh cho các giao dịch Pending
            var allPackages = await _context.MembershipPackages.ToListAsync();
            var allGyms = await _context.Gyms.ToListAsync();
            var allMemberships = await _context.MemberMemberships.ToListAsync();

            // Lọc các giao dịch thuộc quyền quản lý của Owner này
            var ownerTransactions = new List<OwnerTransactionItemViewModel>();

            foreach (var t in rawTransactions)
            {
                int resolvedGymId = 0;
                string resolvedGymName = "—";
                string resolvedPackageName = "—";

                if (t.Membership != null)
                {
                    resolvedGymId = t.Membership.GymId;
                    resolvedGymName = t.Membership.Gym?.Name ?? "—";
                    resolvedPackageName = t.Membership.Package?.Name ?? "—";
                }
                else if (!string.IsNullOrEmpty(t.VnpTxnRef))
                {
                    var parts = t.VnpTxnRef.Split('|');
                    if (parts.Length >= 3 && parts[0] == "BUY" && int.TryParse(parts[1], out int pId) && int.TryParse(parts[2], out int gId))
                    {
                        resolvedGymId = gId;
                        var g = allGyms.FirstOrDefault(x => x.Id == gId);
                        var p = allPackages.FirstOrDefault(x => x.Id == pId);
                        if (g != null) resolvedGymName = g.Name;
                        if (p != null) resolvedPackageName = p.Name;
                    }
                    else if (parts.Length >= 3 && parts[0] == "RENEW" && int.TryParse(parts[1], out int mId) && int.TryParse(parts[2], out int rPkgId))
                    {
                        var mem = allMemberships.FirstOrDefault(x => x.Id == mId);
                        if (mem != null)
                        {
                            resolvedGymId = mem.GymId;
                            var g = allGyms.FirstOrDefault(x => x.Id == mem.GymId);
                            if (g != null) resolvedGymName = g.Name;
                        }
                        var p = allPackages.FirstOrDefault(x => x.Id == rPkgId);
                        if (p != null) resolvedPackageName = $"[Gia hạn] {p.Name}";
                    }
                }

                // Chỉ lấy giao dịch nếu thuộc phòng gym của Owner này
                if (myGymIds.Contains(resolvedGymId))
                {
                    ownerTransactions.Add(new OwnerTransactionItemViewModel
                    {
                        TransactionId = t.Id,
                        MemberName    = t.Member?.FullName ?? "Hội viên",
                        MemberEmail   = t.Member?.Email ?? "—",
                        GymName       = resolvedGymName,
                        PackageName   = resolvedPackageName,
                        Amount        = t.Amount,
                        Status        = t.Status,
                        PaymentMethod = t.PaymentMethod,
                        VnpTxnRef     = t.VnpTxnRef,
                        CreatedAt     = t.CreatedAt,
                        InvoiceCode   = t.Invoice?.InvoiceCode
                    });
                }
            }

            // Áp dụng bộ lọc
            var filtered = ownerTransactions.AsEnumerable();

            if (gymId.HasValue && gymId.Value > 0)
            {
                var targetGym = myGyms.FirstOrDefault(g => g.Id == gymId.Value);
                if (targetGym != null)
                {
                    filtered = filtered.Where(t => t.GymName == targetGym.Name);
                }
            }

            if (!string.IsNullOrEmpty(status))
            {
                filtered = filtered.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (fromDate.HasValue)
            {
                filtered = filtered.Where(t => t.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                filtered = filtered.Where(t => t.CreatedAt <= endOfDay);
            }

            var resultList = filtered.ToList();

            var vm = new OwnerTransactionListViewModel
            {
                SelectedGymId  = gymId,
                SelectedStatus = status,
                FromDate       = fromDate,
                ToDate         = toDate,
                MyGyms         = myGyms,
                Transactions   = resultList
            };

            return View(vm);
        }

        // ==================== DUYỆT GIAO DỊCH THỦ CÔNG DÀNH CHO OWNER ====================
        // POST: /OwnerTransaction/ApproveTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTransaction(int transactionId)
        {
            var userId = await GetCurrentUserIdAsync();
            var transaction = await _context.Transactions
                .Include(t => t.Member)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.Status == "Pending");

            if (transaction == null)
            {
                TempData["Error"] = "Không tìm thấy giao dịch hoặc giao dịch này đã được duyệt trước đó.";
                return RedirectToAction("Index");
            }

            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId)
                .Select(g => g.Id)
                .ToListAsync();

            var parts = (transaction.VnpTxnRef ?? "").Split('|');
            bool isMyGym = false;

            if (parts.Length >= 3 && parts[0] == "BUY" && int.TryParse(parts[2], out int gymId))
            {
                isMyGym = myGyms.Contains(gymId);
            }
            else if (parts.Length >= 3 && parts[0] == "RENEW" && int.TryParse(parts[1], out int memId))
            {
                var mem = await _context.MemberMemberships.FindAsync(memId);
                isMyGym = mem != null && myGyms.Contains(mem.GymId);
            }

            if (!isMyGym)
            {
                TempData["Error"] = "Bạn không có quyền duyệt giao dịch của phòng Gym khác.";
                return RedirectToAction("Index");
            }

            // Kích hoạt giao dịch cho Hội viên
            if (parts.Length >= 3 && parts[0] == "BUY")
            {
                int pkgId = int.Parse(parts[1]);
                int gId = int.Parse(parts[2]);
                var pkg = await _context.MembershipPackages.FindAsync(pkgId);
                var gym = await _context.Gyms.FindAsync(gId);

                if (pkg != null && gym != null)
                {
                    var membership = new MemberMembership
                    {
                        MemberId = transaction.MemberId,
                        GymId = gym.Id,
                        PackageId = pkg.Id,
                        StartDate = DateTime.Today,
                        EndDate = MembershipHelper.CalculateEndDate(pkg.PackageType, pkg.DurationInMonths),
                        PurchaseDate = DateTime.Now,
                        PriceAtPurchase = pkg.Price
                    };
                    _context.MemberMemberships.Add(membership);
                    await _context.SaveChangesAsync();

                    transaction.MembershipId = membership.Id;
                    transaction.Status = "Success";
                    transaction.PaymentMethod = "VietQR (Chủ Gym duyệt)";

                    var invoice = new Invoice
                    {
                        TransactionId = transaction.Id,
                        InvoiceCode = MembershipHelper.GenerateInvoiceCode(),
                        IssuedDate = DateTime.Now,
                        PdfUrl = string.Empty
                    };
                    _context.Invoices.Add(invoice);

                    _context.SystemLogs.Add(new SystemLog
                    {
                        UserId = userId,
                        Action = "TransactionApprovedByOwner",
                        Entity = "Transaction",
                        EntityId = transaction.Id.ToString(),
                        Level = "Info",
                        Description = $"Chủ phòng Gym đã xác nhận thành công giao dịch #{transaction.Id} ({transaction.Amount:N0} đ) cho hội viên {transaction.Member?.FullName} ({transaction.Member?.Email}).",
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã duyệt thành công giao dịch #{transaction.Id} và kích hoạt gói tập cho Hội viên!";
                }
            }
            else if (parts.Length >= 3 && parts[0] == "RENEW")
            {
                int mId = int.Parse(parts[1]);
                int pkgId = int.Parse(parts[2]);
                var mem = await _context.MemberMemberships.Include(m => m.Gym).FirstOrDefaultAsync(m => m.Id == mId);
                var pkg = await _context.MembershipPackages.FindAsync(pkgId);

                if (mem != null && pkg != null)
                {
                    var newEndDate = MembershipHelper.CalculateRenewEndDate(mem.EndDate, pkg.PackageType, pkg.DurationInMonths);
                    mem.EndDate = newEndDate;
                    mem.PackageId = pkg.Id;
                    mem.PriceAtPurchase = pkg.Price;

                    transaction.MembershipId = mem.Id;
                    transaction.Status = "Success";
                    transaction.PaymentMethod = "VietQR (Chủ Gym duyệt)";

                    var invoice = new Invoice
                    {
                        TransactionId = transaction.Id,
                        InvoiceCode = MembershipHelper.GenerateInvoiceCode(),
                        IssuedDate = DateTime.Now,
                        PdfUrl = string.Empty
                    };
                    _context.Invoices.Add(invoice);

                    _context.SystemLogs.Add(new SystemLog
                    {
                        UserId = userId,
                        Action = "TransactionApprovedByOwner",
                        Entity = "Transaction",
                        EntityId = transaction.Id.ToString(),
                        Level = "Info",
                        Description = $"Chủ phòng Gym đã duyệt gia hạn thành công gói \"{pkg.Name}\" cho hội viên {transaction.Member?.FullName}. Hạn mới: {newEndDate:dd/MM/yyyy}.",
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã duyệt gia hạn thành công giao dịch #{transaction.Id}!";
                }
            }

            return RedirectToAction("Index");
        }
    }
}
