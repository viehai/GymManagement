using System;
using System.Linq;
using System.Threading.Tasks;
using GymManagement.Helpers;
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

            var allPackages = await _context.MembershipPackages.ToListAsync();
            var allGyms = await _context.Gyms.ToListAsync();
            var allMemberships = await _context.MemberMemberships.ToListAsync();

            // 3. Tính toán KPI Toàn sàn
            decimal totalRevenue = rawTransactions
                .Where(t => string.Equals(t.Status, "Success", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            int totalTransactions = rawTransactions.Count;
            int successCount = rawTransactions.Count(t => string.Equals(t.Status, "Success", StringComparison.OrdinalIgnoreCase));
            int pendingCount = rawTransactions.Count(t => string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            int failedCount = rawTransactions.Count(t => string.Equals(t.Status, "Failed", StringComparison.OrdinalIgnoreCase));

            // 4. Chuẩn hóa dữ liệu item
            var itemsList = new List<AdminTransactionItemViewModel>();
            foreach (var t in rawTransactions)
            {
                int gymIdVal = 0;
                string gymNameVal = "—";
                string gymAddressVal = "—";
                string packageNameVal = "—";
                string packageTypeVal = "Daily";
                int? durationVal = null;

                if (t.Membership != null)
                {
                    gymIdVal = t.Membership.GymId;
                    gymNameVal = t.Membership.Gym?.Name ?? "—";
                    gymAddressVal = t.Membership.Gym?.Address ?? "—";
                    packageNameVal = t.Membership.Package?.Name ?? "—";
                    packageTypeVal = t.Membership.Package?.PackageType ?? "Daily";
                    durationVal = t.Membership.Package?.DurationInMonths;
                }
                else if (!string.IsNullOrEmpty(t.VnpTxnRef))
                {
                    var parts = t.VnpTxnRef.Split('|');
                    if (parts.Length >= 3 && parts[0] == "BUY" && int.TryParse(parts[1], out int pId) && int.TryParse(parts[2], out int gId))
                    {
                        gymIdVal = gId;
                        var g = allGyms.FirstOrDefault(x => x.Id == gId);
                        var p = allPackages.FirstOrDefault(x => x.Id == pId);
                        if (g != null) { gymNameVal = g.Name; gymAddressVal = g.Address; }
                        if (p != null) { packageNameVal = p.Name; packageTypeVal = p.PackageType; durationVal = p.DurationInMonths; }
                    }
                    else if (parts.Length >= 3 && parts[0] == "RENEW" && int.TryParse(parts[1], out int mId) && int.TryParse(parts[2], out int rPkgId))
                    {
                        var mem = allMemberships.FirstOrDefault(x => x.Id == mId);
                        if (mem != null)
                        {
                            gymIdVal = mem.GymId;
                            var g = allGyms.FirstOrDefault(x => x.Id == mem.GymId);
                            if (g != null) { gymNameVal = g.Name; gymAddressVal = g.Address; }
                        }
                        var p = allPackages.FirstOrDefault(x => x.Id == rPkgId);
                        if (p != null) { packageNameVal = $"[Gia hạn] {p.Name}"; packageTypeVal = p.PackageType; durationVal = p.DurationInMonths; }
                    }
                }

                itemsList.Add(new AdminTransactionItemViewModel
                {
                    Id = t.Id,
                    MemberId = t.MemberId,
                    MemberFullName = t.Member?.FullName ?? "Hội viên ẩn",
                    MemberEmail = t.Member?.Email ?? string.Empty,
                    GymId = gymIdVal,
                    GymName = gymNameVal,
                    GymAddress = gymAddressVal,
                    PackageName = packageNameVal,
                    PackageType = packageTypeVal,
                    DurationInMonths = durationVal,
                    Amount = t.Amount,
                    Status = char.ToUpper(t.Status[0]) + t.Status.Substring(1).ToLower(),
                    PaymentMethod = t.PaymentMethod ?? "VietQR",
                    VnpTxnRef = t.VnpTxnRef,
                    InvoiceCode = t.Invoice?.InvoiceCode,
                    InvoiceId = t.Invoice?.Id,
                    CreatedAt = t.CreatedAt
                });
            }

            // 5. Áp dụng bộ lọc
            var query = itemsList.AsEnumerable();

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(t => t.GymId == gymId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = query.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(t =>
                    (t.MemberFullName != null && t.MemberFullName.ToLower().Contains(kw)) ||
                    (t.MemberEmail != null && t.MemberEmail.ToLower().Contains(kw)) ||
                    (t.InvoiceCode != null && t.InvoiceCode.ToLower().Contains(kw)) ||
                    (t.VnpTxnRef != null && t.VnpTxnRef.ToLower().Contains(kw)) ||
                    (t.GymName != null && t.GymName.ToLower().Contains(kw)) ||
                    (t.PackageName != null && t.PackageName.ToLower().Contains(kw))
                );
            }

            var items = query.ToList();

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

        // ==================== DUYỆT GIAO DỊCH THỦ CÔNG DÀNH CHO ADMIN ====================
        // POST: /AdminTransaction/ApproveTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTransaction(int transactionId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Member)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.Status == "Pending");

            if (transaction == null)
            {
                TempData["Error"] = "Không tìm thấy giao dịch hoặc giao dịch này đã được duyệt trước đó.";
                return RedirectToAction("Index");
            }

            var parts = (transaction.VnpTxnRef ?? "").Split('|');

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
                    transaction.PaymentMethod = "VietQR (Admin duyệt)";

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
                        Action = "TransactionApprovedByAdmin",
                        Entity = "Transaction",
                        EntityId = transaction.Id.ToString(),
                        Level = "Info",
                        Description = $"Admin đã xác nhận duyệt thành công giao dịch #{transaction.Id} ({transaction.Amount:N0} đ) cho hội viên {transaction.Member?.FullName} ({transaction.Member?.Email}).",
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
                    transaction.PaymentMethod = "VietQR (Admin duyệt)";

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
                        Action = "TransactionApprovedByAdmin",
                        Entity = "Transaction",
                        EntityId = transaction.Id.ToString(),
                        Level = "Info",
                        Description = $"Admin đã duyệt gia hạn thành công gói \"{pkg.Name}\" cho hội viên {transaction.Member?.FullName}. Hạn mới: {newEndDate:dd/MM/yyyy}.",
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
