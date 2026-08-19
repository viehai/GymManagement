using System.ComponentModel.DataAnnotations;
using GymManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerPackageController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerPackageController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id ?? string.Empty;
        }

        // ==================== INDEX ====================
        public async Task<IActionResult> Index(int? gymId)
        {
            var userId = await GetCurrentUserIdAsync();

            // Lấy danh sách Gym của Owner hiện tại (đã approved)
            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .ToListAsync();

            ViewBag.MyGyms = myGyms;
            ViewBag.SelectedGymId = gymId;

            IQueryable<MembershipPackage> query = _context.MembershipPackages
                .Include(p => p.Gym)
                .Where(p => p.Gym.OwnerId == userId);

            if (gymId.HasValue)
                query = query.Where(p => p.GymId == gymId.Value);

            var packages = await query.OrderBy(p => p.GymId).ThenBy(p => p.Name).ToListAsync();
            return View(packages);
        }

        // ==================== CREATE ====================
        [HttpGet]
        public async Task<IActionResult> Create(int? gymId)
        {
            var userId = await GetCurrentUserIdAsync();
            await PopulateGymDropdown(userId, gymId);
            return View(new MembershipPackage { GymId = gymId ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MembershipPackage model)
        {
            var userId = await GetCurrentUserIdAsync();

            // Xóa validation lỗi của navigation properties (không được post qua form)
            ModelState.Remove("Gym");
            ModelState.Remove("MemberMemberships");

            // Kiểm tra Gym thuộc về Owner hiện tại
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == model.GymId && g.OwnerId == userId);
            if (gym == null)
            {
                ModelState.AddModelError("GymId", "Phòng Gym không hợp lệ.");
            }

            // Validate duration based on package type
            if (model.PackageType == "Monthly" && (!model.DurationInMonths.HasValue || model.DurationInMonths <= 0))
            {
                ModelState.AddModelError("DurationInMonths", "Vui lòng nhập số tháng hợp lệ cho gói Monthly.");
            }
            if (model.PackageType == "Daily")
            {
                model.DurationInMonths = null;
            }

            if (!ModelState.IsValid)
            {
                await PopulateGymDropdown(userId, model.GymId);
                return View(model);
            }

            _context.MembershipPackages.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gói dịch vụ đã được tạo thành công.";
            return RedirectToAction("Index", new { gymId = model.GymId });
        }

        // ==================== EDIT ====================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var package = await _context.MembershipPackages
                .Include(p => p.Gym)
                .FirstOrDefaultAsync(p => p.Id == id && p.Gym.OwnerId == userId);

            if (package == null) return NotFound();

            await PopulateGymDropdown(userId, package.GymId);
            return View(package);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MembershipPackage model)
        {
            var userId = await GetCurrentUserIdAsync();
            var package = await _context.MembershipPackages
                .Include(p => p.Gym)
                .FirstOrDefaultAsync(p => p.Id == id && p.Gym.OwnerId == userId);

            if (package == null) return NotFound();

            // Xóa validation lỗi của navigation properties (không được post qua form)
            ModelState.Remove("Gym");
            ModelState.Remove("MemberMemberships");

            if (model.PackageType == "Monthly" && (!model.DurationInMonths.HasValue || model.DurationInMonths <= 0))
            {
                ModelState.AddModelError("DurationInMonths", "Vui lòng nhập số tháng hợp lệ cho gói Monthly.");
            }
            if (model.PackageType == "Daily")
            {
                model.DurationInMonths = null;
            }

            if (!ModelState.IsValid)
            {
                await PopulateGymDropdown(userId, model.GymId);
                return View(model);
            }

            package.Name = model.Name;
            package.PackageType = model.PackageType;
            package.DurationInMonths = model.DurationInMonths;
            package.Price = model.Price;
            package.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Gói dịch vụ đã được cập nhật.";
            return RedirectToAction("Index", new { gymId = package.GymId });
        }

        // ==================== DELETE ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var package = await _context.MembershipPackages
                .Include(p => p.Gym)
                .FirstOrDefaultAsync(p => p.Id == id && p.Gym.OwnerId == userId);

            if (package == null) return NotFound();

            int gymId = package.GymId;
            _context.MembershipPackages.Remove(package);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gói dịch vụ đã được xóa.";
            return RedirectToAction("Index", new { gymId });
        }

        // ==================== TOGGLE ACTIVE ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var package = await _context.MembershipPackages
                .Include(p => p.Gym)
                .FirstOrDefaultAsync(p => p.Id == id && p.Gym.OwnerId == userId);

            if (package == null) return NotFound();

            package.IsActive = !package.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = package.IsActive ? "Gói đã được kích hoạt." : "Gói đã được tạm dừng.";
            return RedirectToAction("Index", new { gymId = package.GymId });
        }

        private async Task PopulateGymDropdown(string userId, int? selectedGymId)
        {
            var gyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .ToListAsync();

            ViewBag.GymSelectList = new SelectList(gyms, "Id", "Name", selectedGymId);
        }
    }
}
