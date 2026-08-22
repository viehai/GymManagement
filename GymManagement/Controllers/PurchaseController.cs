using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Luồng mua vé của Member: DailyPass → Package → Checkout → VietQR Payment (SePay) → Result.
    /// </summary>
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public PurchaseController(GymDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
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

            // Chặn Chủ phòng Gym tự mua vé của chính mình
            var user = await _userManager.GetUserAsync(User);
            if (user != null && gym.OwnerId == user.Id)
            {
                TempData["Error"] = "Bạn là Chủ sở hữu của cơ sở phòng Gym này nên không thể tự mua gói vé của chính mình.";
                return RedirectToAction("Details", "Gym", new { id = gymId });
            }

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
                GymId            = gym.Id,
                GymName          = gym.Name,
                GymAddress       = gym.Address,
                GymImage         = gym.ImageUrl,
                PackageId        = dailyPackage.Id,
                PackageName      = dailyPackage.Name,
                PackageType      = "Daily",
                DurationInMonths = null,
                Price            = dailyPackage.Price
            };

            TempData["Checkout"] = JsonSerializer.Serialize(vm);
            return RedirectToAction("Checkout");
        }

        // ═══════════════════════════════════════════════
        // MEM-07: CHỌN GÓI THÁNG
        // GET  /Purchase/Package/{gymId}
        // POST /Purchase/Package
        // ═══════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Package(int gymId)
        {
            var gym = await _context.Gyms
                .Include(g => g.MembershipPackages)
                .FirstOrDefaultAsync(g => g.Id == gymId && g.Status == "Approved");

            if (gym == null) return NotFound();

            // Chặn Chủ phòng Gym tự mua gói của chính mình
            var user = await _userManager.GetUserAsync(User);
            if (user != null && gym.OwnerId == user.Id)
            {
                TempData["Error"] = "Bạn là Chủ sở hữu của cơ sở phòng Gym này nên không thể tự mua gói vé của chính mình.";
                return RedirectToAction("Details", "Gym", new { id = gymId });
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Package(int gymId, int packageId)
        {
            var gym = await _context.Gyms
                .FirstOrDefaultAsync(g => g.Id == gymId && g.Status == "Approved");
            var pkg = await _context.MembershipPackages
                .FirstOrDefaultAsync(p => p.Id == packageId && p.GymId == gymId && p.IsActive);

            if (gym == null || pkg == null) return NotFound();

            // Chặn Chủ phòng Gym tự mua gói của chính mình
            var user = await _userManager.GetUserAsync(User);
            if (user != null && gym.OwnerId == user.Id)
            {
                TempData["Error"] = "Bạn là Chủ sở hữu của cơ sở phòng Gym này nên không thể tự mua gói vé của chính mình.";
                return RedirectToAction("Details", "Gym", new { id = gymId });
            }

            var vm = new PurchaseCheckoutViewModel
            {
                GymId            = gym.Id,
                GymName          = gym.Name,
                GymAddress       = gym.Address,
                GymImage         = gym.ImageUrl ?? string.Empty,
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
        // POST /Purchase/Checkout → Tạo URL VNPay và chuyển hướng
        // ═══════════════════════════════════════════════

        [HttpGet]
        public IActionResult Checkout()
        {
            if (TempData["Checkout"] is not string json)
            {
                TempData["Error"] = "Phiên chọn gói đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("Search", "Gym");
            }

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

            var gym = await _context.Gyms
                .FirstOrDefaultAsync(g => g.Id == vm.GymId && g.Status == "Approved");
            var pkg = await _context.MembershipPackages
                .FirstOrDefaultAsync(p => p.Id == vm.PackageId && p.GymId == vm.GymId && p.IsActive);

            if (gym == null || pkg == null)
            {
                TempData["Error"] = "Gói vé không còn hợp lệ. Vui lòng chọn lại.";
                return RedirectToAction("Search", "Gym");
            }

            if (gym.OwnerId == user.Id)
            {
                TempData["Error"] = "Giao dịch không hợp lệ: Không thể tự mua gói tập tại phòng Gym do chính bạn sở hữu.";
                return RedirectToAction("Details", "Gym", new { id = vm.GymId });
            }

            // Tạo bản ghi Transaction ở trạng thái Pending kèm thông tin gói cần mua trong VnpTxnRef
            var transaction = new Transaction
            {
                MemberId      = user.Id,
                Amount        = pkg.Price,
                Status        = "Pending",
                VnpTxnRef     = $"BUY|{pkg.Id}|{gym.Id}",
                PaymentMethod = "VietQR",
                CreatedAt     = DateTime.Now
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Chuyển hướng sang màn hình Quét mã VietQR
            return RedirectToAction("QrPayment", new { transactionId = transaction.Id });
        }

        // ═══════════════════════════════════════════════
        // VIETQR: MÀN HÌNH QUÉT MÃ QR THANH TOÁN
        // GET /Purchase/QrPayment/{transactionId}
        // ═══════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> QrPayment(int transactionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.MemberId == user.Id);

            if (transaction == null) return NotFound();

            // Nếu đã thanh toán rồi, chuyển thẳng sang trang Kết quả
            if (transaction.Status == "Success")
            {
                return RedirectToAction("Result", new { transactionId = transaction.Id });
            }

            var bankId = _configuration["VietQrSettings:BankId"] ?? "MB";
            var bankName = _configuration["VietQrSettings:BankName"] ?? "MBBank (Ngân hàng Quân Đội)";
            var accountNumber = _configuration["VietQrSettings:AccountNumber"] ?? "0987654321";
            var accountName = _configuration["VietQrSettings:AccountName"] ?? "NGUYEN VAN A";
            var template = _configuration["VietQrSettings:Template"] ?? "compact2";

            string transferContent = $"GP{transaction.Id}";
            string encodedAccountName = Uri.EscapeDataString(accountName);
            string encodedContent = Uri.EscapeDataString(transferContent);
            string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNumber}-{template}.png?amount={((long)transaction.Amount)}&addInfo={encodedContent}&accountName={encodedAccountName}";

            string gymName = "GymPro Management";
            string gymAddress = "";
            string packageName = "Gói dịch vụ";
            string packageType = "Daily";
            int? durationInMonths = null;

            var parts = (transaction.VnpTxnRef ?? "").Split('|');
            if (parts.Length >= 3 && parts[0] == "BUY" && int.TryParse(parts[1], out int pId) && int.TryParse(parts[2], out int gId))
            {
                var pkg = await _context.MembershipPackages.FindAsync(pId);
                var gym = await _context.Gyms.FindAsync(gId);
                if (pkg != null) { packageName = pkg.Name; packageType = pkg.PackageType; durationInMonths = pkg.DurationInMonths; }
                if (gym != null) { gymName = gym.Name; gymAddress = gym.Address; }
            }
            else if (parts.Length >= 3 && parts[0] == "RENEW" && int.TryParse(parts[1], out int mId) && int.TryParse(parts[2], out int rPkgId))
            {
                var mem = await _context.MemberMemberships.Include(m => m.Gym).FirstOrDefaultAsync(m => m.Id == mId);
                var pkg = await _context.MembershipPackages.FindAsync(rPkgId);
                if (pkg != null) { packageName = $"[Gia hạn] {pkg.Name}"; packageType = pkg.PackageType; durationInMonths = pkg.DurationInMonths; }
                if (mem?.Gym != null) { gymName = mem.Gym.Name; gymAddress = mem.Gym.Address; }
            }

            var vm = new QrPaymentViewModel
            {
                TransactionId   = transaction.Id,
                OrderRef        = transferContent,
                Amount          = transaction.Amount,
                BankId          = bankId,
                BankName        = bankName,
                AccountNumber   = accountNumber,
                AccountName     = accountName,
                TransferContent = transferContent,
                QrImageUrl      = qrUrl,
                GymName         = gymName,
                GymAddress      = gymAddress,
                PackageName     = packageName,
                PackageType     = packageType,
                DurationInMonths = durationInMonths,
                CreatedAt       = transaction.CreatedAt
            };

            return View(vm);
        }

        // ═══════════════════════════════════════════════
        // POLLING API: KIỂM TRA TRẠNG THÁI THANH TOÁN (AJAX)
        // GET /Purchase/CheckPaymentStatus?transactionId=...
        // ═══════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(int transactionId)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
            {
                return Json(new { success = false, isPaid = false, message = "Không tìm thấy giao dịch." });
            }

            if (transaction.Status == "Success")
            {
                return Json(new
                {
                    success     = true,
                    isPaid      = true,
                    redirectUrl = Url.Action("Result", new { transactionId = transaction.Id })
                });
            }

            return Json(new { success = true, isPaid = false, status = transaction.Status });
        }

        // ═══════════════════════════════════════════════
        // SEPAY WEBHOOK: TỰ ĐỘNG NHẬN BIẾN ĐỘNG SỐ DƯ TỪ NGÂN HÀNG
        // POST /Purchase/SepayWebhook
        // ═══════════════════════════════════════════════

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SepayWebhook([FromBody] SepayWebhookDto payload)
        {
            if (payload == null)
            {
                return BadRequest(new { success = false, message = "Payload trống" });
            }

            // Kiểm tra API Key bảo mật nếu được cấu hình
            var configuredApiKey = _configuration["SePaySettings:ApiKey"];
            if (!string.IsNullOrEmpty(configuredApiKey))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!authHeader.Contains(configuredApiKey, StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized(new { success = false, message = "API Key không hợp lệ" });
                }
            }

            // Trích xuất mã giao dịch từ nội dung chuyển khoản (Regex bắt GP123 hoặc GYMPRO123)
            string rawText = $"{payload.Content} {payload.Description}";
            var match = System.Text.RegularExpressions.Regex.Match(
                rawText, @"(?:GP|GYMPRO|GP_)\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            int transactionId = 0;
            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out transactionId);
            }

            if (transactionId <= 0)
            {
                return Ok(new { success = false, message = "Không tìm thấy mã giao dịch GymPro trong nội dung chuyển tiền." });
            }

            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
            {
                return Ok(new { success = false, message = $"Không tìm thấy giao dịch #{transactionId}." });
            }

            if (transaction.Status == "Success")
            {
                return Ok(new { success = true, message = "Giao dịch đã được xác nhận trước đó." });
            }

            // Kiểm tra số tiền nhận được có đủ hay không
            if (payload.TransferAmount < transaction.Amount)
            {
                _context.SystemLogs.Add(new SystemLog
                {
                    UserId = transaction.MemberId,
                    Action = "SecurityAlert",
                    Entity = "Transaction",
                    EntityId = transaction.Id.ToString(),
                    Level = "Warning",
                    Description = $"Chuyển khoản VietQR thiếu tiền cho GD #{transaction.Id}. Cần thanh toán: {transaction.Amount:N0} đ, thực nhận: {payload.TransferAmount:N0} đ.",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                return Ok(new { success = false, message = "Số tiền chuyển khoản nhỏ hơn giá trị đơn hàng." });
            }

            bool completed = await CompletePaymentAsync(transaction, $"VietQR ({payload.Gateway ?? "Ngân hàng"})");
            if (completed)
            {
                return Ok(new { success = true, message = "Thanh toán thành công và đã kích hoạt gói tập." });
            }

            return BadRequest(new { success = false, message = "Kích hoạt gói tập thất bại." });
        }

        // ═══════════════════════════════════════════════
        // NÚT DỰ PHÒNG: TÔI ĐÃ CHUYỂN TIỀN XONG
        // POST /Purchase/ConfirmManualPayment
        // ═══════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmManualPayment(int transactionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.MemberId == user.Id);

            if (transaction == null) return NotFound();

            if (transaction.Status == "Success")
            {
                return RedirectToAction("Result", new { transactionId = transaction.Id });
            }

            bool completed = await CompletePaymentAsync(transaction, "VietQR (Xác nhận thủ công)");
            if (completed)
            {
                TempData["Success"] = "Đã xác nhận thanh toán thành công!";
                return RedirectToAction("Result", new { transactionId = transaction.Id });
            }

            TempData["Error"] = "Không thể xử lý giao dịch. Vui lòng thử lại.";
            return RedirectToAction("QrPayment", new { transactionId });
        }

        // ═══════════════════════════════════════════════
        // HELPER XỬ LÝ KÍCH HOẠT GÓI KHI THANH TOÁN THÀNH CÔNG
        // ═══════════════════════════════════════════════

        private async Task<bool> CompletePaymentAsync(Transaction transaction, string paymentSource = "VietQR")
        {
            if (transaction.Status == "Success") return true;

            var parts = (transaction.VnpTxnRef ?? "").Split('|');
            string orderType = parts.Length > 0 ? parts[0] : "";

            if (orderType == "BUY" && parts.Length >= 3)
            {
                int pkgId = int.Parse(parts[1]);
                int gymId = int.Parse(parts[2]);

                var pkg = await _context.MembershipPackages.FindAsync(pkgId);
                var gym = await _context.Gyms.FindAsync(gymId);

                if (pkg != null && gym != null)
                {
                    var startDate = DateTime.Today;
                    var endDate = MembershipHelper.CalculateEndDate(pkg.PackageType, pkg.DurationInMonths);

                    var membership = new MemberMembership
                    {
                        MemberId = transaction.MemberId,
                        GymId = gym.Id,
                        PackageId = pkg.Id,
                        StartDate = startDate,
                        EndDate = endDate,
                        PurchaseDate = DateTime.Now,
                        PriceAtPurchase = pkg.Price
                    };
                    _context.MemberMemberships.Add(membership);
                    await _context.SaveChangesAsync();

                    transaction.MembershipId = membership.Id;
                    transaction.Status = "Success";
                    transaction.PaymentMethod = paymentSource;

                    var invoice = new Invoice
                    {
                        TransactionId = transaction.Id,
                        InvoiceCode = MembershipHelper.GenerateInvoiceCode(),
                        IssuedDate = DateTime.Now,
                        PdfUrl = string.Empty
                    };
                    _context.Invoices.Add(invoice);

                    var member = await _userManager.FindByIdAsync(transaction.MemberId);
                    _context.SystemLogs.Add(new SystemLog
                    {
                        UserId = transaction.MemberId,
                        Action = "PaymentSuccess",
                        Entity = "Transaction",
                        EntityId = transaction.Id.ToString(),
                        Level = "Info",
                        Description = $"Hội viên {member?.FullName} ({member?.Email}) đã thanh toán thành công {pkg.Price:N0} VNĐ qua {paymentSource} cho gói \"{pkg.Name}\" tại \"{gym.Name}\".",
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            else if (orderType == "RENEW" && parts.Length >= 3)
            {
                int memId = int.Parse(parts[1]);
                int pkgId = int.Parse(parts[2]);

                var membership = await _context.MemberMemberships
                    .Include(m => m.Gym)
                    .FirstOrDefaultAsync(m => m.Id == memId);
                var pkg = await _context.MembershipPackages.FindAsync(pkgId);

                if (membership != null && pkg != null)
                {
                    var newEndDate = MembershipHelper.CalculateRenewEndDate(
                        membership.EndDate, pkg.PackageType, pkg.DurationInMonths);

                    membership.EndDate = newEndDate;
                    membership.PackageId = pkg.Id;
                    membership.PriceAtPurchase = pkg.Price;

                    transaction.MembershipId = membership.Id;
                    transaction.Status = "Success";
                    transaction.PaymentMethod = paymentSource;

                    var invoice = new Invoice
                    {
                        TransactionId = transaction.Id,
                        InvoiceCode = MembershipHelper.GenerateInvoiceCode(),
                        IssuedDate = DateTime.Now,
                        PdfUrl = string.Empty
                    };
                    _context.Invoices.Add(invoice);

                    var member = await _userManager.FindByIdAsync(transaction.MemberId);
                    _context.SystemLogs.Add(new SystemLog
                    {
                        UserId = transaction.MemberId,
                        Action = "MembershipRenewed",
                        Entity = "MemberMembership",
                        EntityId = membership.Id.ToString(),
                        Level = "Info",
                        Description = $"Hội viên {member?.FullName} đã gia hạn gói \"{pkg.Name}\" ({pkg.Price:N0} VNĐ) qua {paymentSource} tại \"{membership.Gym?.Name}\". Hạn mới: {newEndDate:dd/MM/yyyy}.",
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    return true;
                }
            }

            return false;
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
        // POST /Purchase/Renew/{membershipId} → VNPay Gateway
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

            // Tạo Transaction gia hạn ở trạng thái Pending
            var transaction = new Transaction
            {
                MemberId      = user.Id,
                MembershipId  = membership.Id,
                Amount        = pkg.Price,
                Status        = "Pending",
                VnpTxnRef     = $"RENEW|{membership.Id}|{pkg.Id}",
                PaymentMethod = "VietQR",
                CreatedAt     = DateTime.Now
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("QrPayment", new { transactionId = transaction.Id });
        }
    }
}
