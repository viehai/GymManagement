using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Luồng mua vé của Member: DailyPass → Package → Checkout → Result.
    /// Yêu cầu đăng nhập (Authorize). Guest bấm CTA sẽ bị redirect về Account/Login.
    /// Mock VNPay: không cần key thật — POST Checkout tự chuyển thành công.
    /// </summary>
    [Authorize(Roles = "Member")]
    public class PurchaseController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PurchaseController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ═══════════════════════════════════════════════
        // MEM-06: MUA VÉ NGÀY
        // GET /Purchase/DailyPass/{gymId}
        // ═══════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> DailyPass(int gymId)
        {
            var gym = await _context.Gyms
                .Include(g => g.MembershipPackages)
                .FirstOrDefaultAsync(g => g.Id == gymId && g.Status == "Approved");

            if (gym == null) return NotFound();

            // Lấy gói Daily đầu tiên còn active
            var dailyPackage = gym.MembershipPackages
                .Where(p => p.IsActive && p.PackageType == "Daily")
                .OrderBy(p => p.Price)
                .FirstOrDefault();

            if (dailyPackage == null)
            {
                TempData["Error"] = "Phòng Gym này hiện không có vé ngày.";
                return RedirectToAction("Details", "Gym", new { id = gymId });
            }

            var vm = new PurchaseCheckoutViewModel
            {
                GymId      = gym.Id,
                GymName    = gym.Name,
                GymAddress = gym.Address,
                GymImage   = gym.ImageUrl ?? string.Empty,
                PackageId         = dailyPackage.Id,
                PackageName       = dailyPackage.Name,
                PackageType       = dailyPackage.PackageType,
                DurationInMonths  = dailyPackage.DurationInMonths,
                Price             = dailyPackage.Price
            };

            return View(vm);
        }

        // POST /Purchase/DailyPass/{gymId} — redirect sang Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DailyPass(int gymId, PurchaseCheckoutViewModel vm)
        {
            TempData["Checkout"] = JsonSerializer.Serialize(vm);
            return RedirectToAction("Checkout");
        }

        // ═══════════════════════════════════════════════
        // MEM-07: CHỌN GÓI THÁNG
        // GET /Purchase/Package/{gymId}
        // ═══════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Package(int gymId)
        {
            var gym = await _context.Gyms
                .Include(g => g.MembershipPackages)
                .FirstOrDefaultAsync(g => g.Id == gymId && g.Status == "Approved");

            if (gym == null) return NotFound();

            var packages = gym.MembershipPackages
                .Where(p => p.IsActive && p.PackageType == "Monthly")
                .OrderBy(p => p.Price)
                .Select(p => new PackageDisplayViewModel
                {
                    Id               = p.Id,
                    Name             = p.Name,
                    PackageType      = p.PackageType,
                    DurationInMonths = p.DurationInMonths,
                    Price            = p.Price
                })
                .ToList();

            if (!packages.Any())
            {
                TempData["Error"] = "Phòng Gym này hiện không có gói tháng.";
                return RedirectToAction("Details", "Gym", new { id = gymId });
            }

            ViewBag.GymId      = gymId;
            ViewBag.GymName    = gym.Name;
            ViewBag.GymAddress = gym.Address;
            ViewBag.GymImage   = gym.ImageUrl ?? string.Empty;

            return View(packages);
        }

        // POST /Purchase/Package — nhận packageId, redirect sang Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Package(int gymId, int packageId)
        {
            var gym = await _context.Gyms
                .FirstOrDefaultAsync(g => g.Id == gymId && g.Status == "Approved");
            var pkg = await _context.MembershipPackages
                .FirstOrDefaultAsync(p => p.Id == packageId && p.GymId == gymId && p.IsActive);

            if (gym == null || pkg == null) return NotFound();

            var vm = new PurchaseCheckoutViewModel
            {
                GymId      = gym.Id,
                GymName    = gym.Name,
                GymAddress = gym.Address,
                GymImage   = gym.ImageUrl ?? string.Empty,
                PackageId        = pkg.Id,
                PackageName      = pkg.Name,
                PackageType      = pkg.PackageType,
                DurationInMonths = pkg.DurationInMonths,
                Price            = pkg.Price
            };

            TempData["Checkout"] = JsonSerializer.Serialize(vm);
            return RedirectToAction("Checkout");
        }

        // ═══════════════════════════════════════════════
        // MEM-08: XÁC NHẬN ĐƠN HÀNG
        // GET  /Purchase/Checkout
        // POST /Purchase/Checkout → Mock success → tạo Membership + Invoice
        // ═══════════════════════════════════════════════

        [HttpGet]
        public IActionResult Checkout()
        {
            if (TempData["Checkout"] is not string json)
            {
                TempData["Error"] = "Phiên chọn gói đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("Search", "Gym");
            }

            // Keep để POST còn đọc được
            TempData.Keep("Checkout");

            var vm = JsonSerializer.Deserialize<PurchaseCheckoutViewModel>(json);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(PurchaseCheckoutViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Kiểm tra Gym + Package còn hợp lệ
            var gym = await _context.Gyms
                .FirstOrDefaultAsync(g => g.Id == vm.GymId && g.Status == "Approved");
            var pkg = await _context.MembershipPackages
                .FirstOrDefaultAsync(p => p.Id == vm.PackageId && p.GymId == vm.GymId && p.IsActive);

            if (gym == null || pkg == null)
            {
                TempData["Error"] = "Gói vé không còn hợp lệ. Vui lòng chọn lại.";
                return RedirectToAction("Search", "Gym");
            }

            // ── Mock VNPay: tạo thẳng Transaction Success ──
            var transaction = new Transaction
            {
                MemberId      = user.Id,
                Amount        = pkg.Price,
                Status        = "Success",
                VnpTxnRef     = $"MOCK-{Guid.NewGuid():N}"[..20],
                PaymentMethod = "VNPay",
                CreatedAt     = DateTime.Now
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // ── Tạo MemberMembership ──
            var startDate = DateTime.Today;
            var endDate   = MembershipHelper.CalculateEndDate(pkg.PackageType, pkg.DurationInMonths);

            var membership = new MemberMembership
            {
                MemberId        = user.Id,
                GymId           = gym.Id,
                PackageId       = pkg.Id,
                StartDate       = startDate,
                EndDate         = endDate,
                PurchaseDate    = DateTime.Now,
                PriceAtPurchase = pkg.Price
            };
            _context.MemberMemberships.Add(membership);
            await _context.SaveChangesAsync();

            // ── Liên kết Transaction → Membership ──
            transaction.MembershipId = membership.Id;

            // ── Tạo Invoice ──
            var invoice = new Invoice
            {
                TransactionId = transaction.Id,
                InvoiceCode   = MembershipHelper.GenerateInvoiceCode(),
                IssuedDate    = DateTime.Now,
                PdfUrl        = string.Empty // HTML view, không tạo file PDF
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return RedirectToAction("Result", new { transactionId = transaction.Id });
        }

        // ═══════════════════════════════════════════════
        // MEM-10: KẾT QUẢ THANH TOÁN
        // GET /Purchase/Result?transactionId=...
        // ═══════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Result(int transactionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var transaction = await _context.Transactions
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Gym)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Package)
                .Include(t => t.Invoice)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.MemberId == user.Id);

            if (transaction == null) return NotFound();

            return View(transaction);
        }

        // ═══════════════════════════════════════════════
        // MEM-15: GIA HẠN VÉ
        // GET  /Purchase/Renew/{membershipId}
        // POST /Purchase/Renew/{membershipId}
        // ═══════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Renew(int membershipId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var membership = await _context.MemberMemberships
                .Include(m => m.Gym)
                .Include(m => m.Package)
                .FirstOrDefaultAsync(m => m.Id == membershipId && m.MemberId == user.Id);

            if (membership == null) return NotFound();

            var activePackages = await _context.MembershipPackages
                .Where(p => p.GymId == membership.GymId && p.IsActive)
                .OrderBy(p => p.PackageType == "Daily" ? 0 : 1)
                .ThenBy(p => p.Price)
                .ToListAsync();

            if (!activePackages.Any())
            {
                TempData["Error"] = "Phòng Gym này hiện không có gói tập nào khả dụng để gia hạn.";
                return RedirectToAction("MembershipDetails", "Member", new { id = membershipId });
            }

            var vm = new RenewMembershipViewModel
            {
                MembershipId       = membership.Id,
                GymId              = membership.GymId,
                GymName            = membership.Gym?.Name ?? "—",
                GymAddress         = membership.Gym?.Address ?? "—",
                GymImage           = membership.Gym?.ImageUrl ?? string.Empty,
                CurrentPackageName = membership.Package?.Name ?? "—",
                CurrentEndDate     = membership.EndDate,
                SelectedPackageId  = activePackages.Any(p => p.Id == membership.PackageId)
                                        ? membership.PackageId
                                        : activePackages.First().Id,
                AvailablePackages  = activePackages.Select(p => new PackageOptionViewModel
                {
                    Id                    = p.Id,
                    Name                  = p.Name,
                    PackageType           = p.PackageType,
                    DurationInMonths      = p.DurationInMonths,
                    Price                 = p.Price,
                    CalculatedNewEndDate  = MembershipHelper.CalculateRenewEndDate(
                                                membership.EndDate, p.PackageType, p.DurationInMonths)
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew(int membershipId, int selectedPackageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var membership = await _context.MemberMemberships
                .Include(m => m.Gym)
                .FirstOrDefaultAsync(m => m.Id == membershipId && m.MemberId == user.Id);

            if (membership == null) return NotFound();

            var pkg = await _context.MembershipPackages
                .FirstOrDefaultAsync(p => p.Id == selectedPackageId && p.GymId == membership.GymId && p.IsActive);

            if (pkg == null)
            {
                TempData["Error"] = "Gói tập được chọn không hợp lệ.";
                return RedirectToAction("Renew", new { membershipId });
            }

            // ── Tính ngày hết hạn mới (cộng dồn nếu còn hạn) ──
            var newEndDate = MembershipHelper.CalculateRenewEndDate(
                membership.EndDate, pkg.PackageType, pkg.DurationInMonths);

            // ── Tạo Transaction gia hạn ──
            var transaction = new Transaction
            {
                MemberId      = user.Id,
                MembershipId  = membership.Id,
                Amount        = pkg.Price,
                Status        = "Success",
                VnpTxnRef     = $"RENEW-{Guid.NewGuid():N}"[..20],
                PaymentMethod = "VNPay",
                CreatedAt     = DateTime.Now
            };
            _context.Transactions.Add(transaction);

            // ── Cập nhật ngày hết hạn của Membership ──
            membership.EndDate          = newEndDate;
            membership.PackageId        = pkg.Id;
            membership.PriceAtPurchase  = pkg.Price;

            await _context.SaveChangesAsync();

            // ── Tạo Invoice gia hạn ──
            var invoice = new Invoice
            {
                TransactionId = transaction.Id,
                InvoiceCode   = MembershipHelper.GenerateInvoiceCode(),
                IssuedDate    = DateTime.Now,
                PdfUrl        = string.Empty
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Gia hạn thành công! Hạn sử dụng mới của bạn là ngày {newEndDate:dd/MM/yyyy}.";
            return RedirectToAction("Result", new { transactionId = transaction.Id });
        }
    }
}
