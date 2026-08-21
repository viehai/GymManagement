using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Quản lý thiết bị của phòng Gym dành cho Chủ phòng Gym / Owner (OWN-06 -> OWN-10).
    /// </summary>
    [Authorize(Roles = "Owner")]
    public class OwnerEquipmentController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public OwnerEquipmentController(
            GymDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // ==================== OWN-10: DANH SÁCH THIẾT BỊ CỦA GYM ====================
        // GET /OwnerEquipment/Index/{gymId?}
        public async Task<IActionResult> Index(int? gymId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == user.Id)
                .OrderBy(g => g.Name)
                .ToListAsync();

            if (!myGyms.Any())
            {
                return View(new OwnerEquipmentListViewModel { MyGyms = new List<Gym>() });
            }

            var targetGym = gymId.HasValue && gymId.Value > 0
                ? myGyms.FirstOrDefault(g => g.Id == gymId.Value) ?? myGyms.First()
                : myGyms.First();

            var equipments = await _context.GymEquipments
                .Include(ge => ge.Equipment)
                .Where(ge => ge.GymId == targetGym.Id)
                .OrderByDescending(ge => ge.Id)
                .ToListAsync();

            var vm = new OwnerEquipmentListViewModel
            {
                CurrentGym = targetGym,
                SelectedGymId = targetGym.Id,
                MyGyms = myGyms,
                Equipments = equipments
            };

            return View(vm);
        }

        // ==================== OWN-06: CATALOG THIẾT BỊ GỐC ====================
        // GET /OwnerEquipment/Catalog?gymId=...
        public async Task<IActionResult> Catalog(int gymId, string? category)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId && g.OwnerId == user.Id);
            if (gym == null) return NotFound();

            // Lấy danh sách ID các máy đã có trong Gym này để loại trừ
            var existingIds = await _context.GymEquipments
                .Where(ge => ge.GymId == gymId && ge.EquipmentId.HasValue)
                .Select(ge => ge.EquipmentId!.Value)
                .ToListAsync();

            var query = _context.Equipments
                .Where(e => !existingIds.Contains(e.Id));

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category == category);
            }

            var availableEquipments = await query
                .OrderBy(e => e.Category)
                .ThenBy(e => e.Name)
                .ToListAsync();

            var allCategories = await _context.Equipments
                .Where(e => !string.IsNullOrEmpty(e.Category))
                .Select(e => e.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var vm = new OwnerEquipmentCatalogViewModel
            {
                CurrentGym = gym,
                GymId = gym.Id,
                AvailableEquipments = availableEquipments,
                SelectedCategory = category,
                Categories = allCategories
            };

            return View(vm);
        }

        // ==================== THÊM THIẾT BỊ TỪ CATALOG GỐC ====================
        // POST /OwnerEquipment/AddFromCatalog
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFromCatalog(int gymId, int equipmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId && g.OwnerId == user.Id);
            if (gym == null) return NotFound();

            var exists = await _context.GymEquipments
                .AnyAsync(ge => ge.GymId == gymId && ge.EquipmentId == equipmentId);

            if (!exists)
            {
                var ge = new GymEquipment
                {
                    GymId = gymId,
                    EquipmentId = equipmentId,
                    IsCustom = false,
                    CustomName = string.Empty,
                    CustomImage = string.Empty,
                    IsVisible = true
                };
                _context.GymEquipments.Add(ge);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã thêm thiết bị từ Catalog vào phòng Gym thành công!";
            }
            else
            {
                TempData["Error"] = "Thiết bị này đã có trong danh mục phòng Gym của bạn.";
            }

            return RedirectToAction(nameof(Index), new { gymId });
        }

        // ==================== OWN-08: THÊM THIẾT BỊ CUSTOM ====================
        // GET /OwnerEquipment/CreateCustom?gymId=...
        [HttpGet]
        public async Task<IActionResult> CreateCustom(int gymId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId && g.OwnerId == user.Id);
            if (gym == null) return NotFound();

            var vm = new OwnerEquipmentCustomViewModel
            {
                GymId = gym.Id,
                GymName = gym.Name
            };

            return View(vm);
        }

        // POST /OwnerEquipment/CreateCustom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustom(OwnerEquipmentCustomViewModel model, IFormFile? imageFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == model.GymId && g.OwnerId == user.Id);
            if (gym == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.CustomName))
            {
                ModelState.AddModelError("CustomName", "Vui lòng nhập tên thiết bị.");
            }

            if (!ModelState.IsValid)
            {
                model.GymName = gym.Name;
                return View(model);
            }

            string? customImagePath = null;

            // Xử lý upload ảnh vật lý vào wwwroot/images/equipments/custom/
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("CustomName", "Chỉ chấp nhận file ảnh định dạng .jpg, .jpeg, .png, .webp, .svg.");
                    model.GymName = gym.Name;
                    return View(model);
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "images", "equipments", "custom");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var newFileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadFolder, newFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                customImagePath = $"/images/equipments/custom/{newFileName}";
            }
            else
            {
                // Fallback default image
                customImagePath = "/images/equipments/strength-multi/dumbbells.png";
            }

            // DB Check Constraint: ([IsCustom] = 1 AND [EquipmentId] IS NULL AND [CustomName] IS NOT NULL)
            var ge = new GymEquipment
            {
                GymId = model.GymId,
                EquipmentId = null,
                IsCustom = true,
                CustomName = model.CustomName.Trim(),
                CustomImage = customImagePath,
                IsVisible = true
            };

            _context.GymEquipments.Add(ge);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm thiết bị tự chọn \"{model.CustomName}\" thành công!";
            return RedirectToAction(nameof(Index), new { gymId = model.GymId });
        }

        // ==================== OWN-07: BẬT / TẮT HIỂN THỊ THIẾT BỊ ====================
        // POST /OwnerEquipment/ToggleVisibility/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var ge = await _context.GymEquipments
                .Include(g => g.Gym)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (ge == null || ge.Gym == null || ge.Gym.OwnerId != user.Id)
            {
                return NotFound();
            }

            ge.IsVisible = !ge.IsVisible;
            await _context.SaveChangesAsync();

            TempData["Success"] = ge.IsVisible
                ? "Đã bật hiển thị thiết bị cho hội viên xem."
                : "Đã ẩn thiết bị khỏi danh mục phòng Gym.";

            return RedirectToAction(nameof(Index), new { gymId = ge.GymId });
        }

        // ==================== OWN-09: XÓA THIẾT BỊ KHỎI GYM ====================
        // POST /OwnerEquipment/Remove/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var ge = await _context.GymEquipments
                .Include(g => g.Gym)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (ge == null || ge.Gym == null || ge.Gym.OwnerId != user.Id)
            {
                return NotFound();
            }

            int gymId = ge.GymId;
            _context.GymEquipments.Remove(ge);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa thiết bị khỏi danh mục phòng Gym thành công!";
            return RedirectToAction(nameof(Index), new { gymId });
        }
    }
}
