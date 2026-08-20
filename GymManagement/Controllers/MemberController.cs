using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Member,Owner")]
    public class MemberController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GymDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MemberController(
            UserManager<ApplicationUser> userManager,
            GymDbContext context,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _context = context;
            _env = env;
        }

        // ==================== TRANG CÁ NHÂN ====================
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == user.Id)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            var activeMembershipsCount = await _context.MemberMemberships
                .CountAsync(m => m.MemberId == user.Id && m.EndDate >= DateTime.Today);

            var transactionCount = await _context.Transactions
                .CountAsync(t => t.MemberId == user.Id);

            ViewBag.Roles = roles;
            ViewBag.Gyms = myGyms;
            ViewBag.ActiveMembershipsCount = activeMembershipsCount;
            ViewBag.TransactionCount = transactionCount;
            return View(user);
        }

        // ==================== ĐĂNG KÝ MỞ PHÒNG GYM ====================
        [HttpGet]
        public IActionResult RegisterGym()
        {
            return View(new RegisterGymViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterGym(RegisterGymViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Upload ảnh nếu có
            string imageUrl = "";
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .webp).");
                    return View(model);
                }

                if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Ảnh không được lớn hơn 5MB.");
                    return View(model);
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "gyms");
                Directory.CreateDirectory(uploadFolder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);

                imageUrl = $"/uploads/gyms/{fileName}";
            }

            var gym = new Gym
            {
                OwnerId = user.Id,
                Name = model.Name,
                Address = model.Address,
                Description = model.Description,
                ImageUrl = imageUrl,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Gyms.Add(gym);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Yêu cầu đăng ký phòng Gym đã được gửi! Chúng tôi sẽ xem xét và phản hồi trong thời gian sớm nhất.";
            return RedirectToAction("Profile");
        }

        // ==================== MEM-11: LỊCH SỬ GIAO DỊCH ====================
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> TransactionHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var transactions = await _context.Transactions
                .Where(t => t.MemberId == user.Id)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Gym)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Package)
                .Include(t => t.Invoice)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var vm = transactions.Select(t => new TransactionHistoryViewModel
            {
                TransactionId    = t.Id,
                GymName          = t.Membership?.Gym?.Name ?? "—",
                PackageName      = t.Membership?.Package?.Name ?? "—",
                PackageTypeLabel = t.Membership?.Package?.PackageType == "Daily"
                                       ? "Vé ngày"
                                       : $"Gói {t.Membership?.Package?.DurationInMonths} tháng",
                Amount    = t.Amount,
                Status    = t.Status,
                CreatedAt = t.CreatedAt,
                InvoiceId = t.Invoice?.Id
            }).ToList();

            return View(vm);
        }

        // ==================== MEM-12: CHI TIẾT HÓA ĐƠN ====================
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> InvoiceDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var invoice = await _context.Invoices
                .Include(i => i.Transaction)
                    .ThenInclude(t => t.Member)
                .Include(i => i.Transaction)
                    .ThenInclude(t => t.Membership)
                        .ThenInclude(m => m.Gym)
                .Include(i => i.Transaction)
                    .ThenInclude(t => t.Membership)
                        .ThenInclude(m => m.Package)
                .FirstOrDefaultAsync(i => i.Id == id && i.Transaction.MemberId == user.Id);

            if (invoice == null) return NotFound();

            var membership = invoice.Transaction.Membership;
            var member     = invoice.Transaction.Member;
            var pkg        = membership?.Package;
            var gym        = membership?.Gym;

            var vm = new InvoiceDetailsViewModel
            {
                InvoiceId        = invoice.Id,
                InvoiceCode      = invoice.InvoiceCode,
                IssuedDate       = invoice.IssuedDate,
                MemberName       = member?.FullName ?? string.Empty,
                MemberEmail      = member?.Email ?? string.Empty,
                GymName          = gym?.Name ?? string.Empty,
                GymAddress       = gym?.Address ?? string.Empty,
                PackageName      = pkg?.Name ?? string.Empty,
                PackageType      = pkg?.PackageType ?? string.Empty,
                DurationInMonths = pkg?.DurationInMonths,
                Amount           = invoice.Transaction.Amount,
                StartDate        = membership?.StartDate ?? DateTime.Today,
                EndDate          = membership?.EndDate ?? DateTime.Today
            };

            return View(vm);
        }
    }
}

