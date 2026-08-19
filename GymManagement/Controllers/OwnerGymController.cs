using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerGymController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public OwnerGymController(
            GymDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id ?? string.Empty;
        }

        // ==================== INDEX ====================
        public async Task<IActionResult> Index()
        {
            var userId = await GetCurrentUserIdAsync();
            var gyms = await _context.Gyms
                .Include(g => g.MembershipPackages)
                .Include(g => g.MemberMemberships)
                .Where(g => g.OwnerId == userId)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(gyms);
        }

        // ==================== DETAILS ====================
        public async Task<IActionResult> Details(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms
                .Include(g => g.MembershipPackages)
                .Include(g => g.GymEquipments).ThenInclude(ge => ge.Equipment)
                .FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == userId);

            if (gym == null) return NotFound();
            return View(gym);
        }

        // ==================== CREATE ====================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RegisterGymViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterGymViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = await GetCurrentUserIdAsync();
            string imageUrl = await SaveImageAsync(model.ImageFile, model) ?? "";

            var gym = new Gym
            {
                OwnerId = userId,
                Name = model.Name,
                Address = model.Address,
                Description = model.Description,
                ImageUrl = imageUrl,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Gyms.Add(gym);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Phòng Gym mới đã được tạo và đang chờ phê duyệt.";
            return RedirectToAction("Index");
        }

        // ==================== EDIT ====================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == userId);
            if (gym == null) return NotFound();

            var model = new RegisterGymViewModel
            {
                Name = gym.Name,
                Address = gym.Address,
                Description = gym.Description
            };
            ViewBag.ExistingImage = gym.ImageUrl;
            ViewBag.GymId = gym.Id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RegisterGymViewModel model)
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == userId);
            if (gym == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ExistingImage = gym.ImageUrl;
                ViewBag.GymId = gym.Id;
                return View(model);
            }

            // Xử lý ảnh mới nếu có
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string? newImageUrl = await SaveImageAsync(model.ImageFile, model);
                if (newImageUrl == null)
                {
                    ViewBag.ExistingImage = gym.ImageUrl;
                    ViewBag.GymId = gym.Id;
                    return View(model);
                }
                // Xóa ảnh cũ
                if (!string.IsNullOrEmpty(gym.ImageUrl))
                    DeleteImage(gym.ImageUrl);

                gym.ImageUrl = newImageUrl;
            }

            gym.Name = model.Name;
            gym.Address = model.Address;
            gym.Description = model.Description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thông tin phòng Gym đã được cập nhật.";
            return RedirectToAction("Index");
        }

        // ==================== DELETE ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == userId);
            if (gym == null) return NotFound();

            if (!string.IsNullOrEmpty(gym.ImageUrl))
                DeleteImage(gym.ImageUrl);

            _context.Gyms.Remove(gym);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Phòng Gym đã được xóa.";
            return RedirectToAction("Index");
        }

        // ==================== HELPERS ====================
        private async Task<string?> SaveImageAsync(IFormFile? file, RegisterGymViewModel model)
        {
            if (file == null || file.Length == 0) return "";

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .webp).");
                return null;
            }
            if (file.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImageFile", "Ảnh không được lớn hơn 5MB.");
                return null;
            }

            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "gyms");
            Directory.CreateDirectory(uploadFolder);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/gyms/{fileName}";
        }

        private void DeleteImage(string imageUrl)
        {
            try
            {
                var path = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch { /* Bỏ qua lỗi xóa file */ }
        }
    }
}
