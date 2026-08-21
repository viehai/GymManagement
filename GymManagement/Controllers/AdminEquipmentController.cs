using GymManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Quản lý danh mục thiết bị / máy tập gốc (Equipment Catalog) của hệ thống GymPro dành cho Admin (ADM-06 -> ADM-09).
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminEquipmentController : Controller
    {
        private readonly GymDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly List<string> EquipmentCategories = new()
        {
            "Cardio",
            "Strength - Ngực (Chest)",
            "Strength - Lưng Xô (Back)",
            "Strength - Chân Mông (Legs & Glutes)",
            "Strength - Tay Vai (Arms & Shoulders)",
            "Strength - Bụng (Core)",
            "Strength - Đa Dụng (Multi-purpose)"
        };

        public AdminEquipmentController(GymDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ==================== ADM-06: DANH SÁCH CATALOG THIẾT BỊ ====================
        // GET /AdminEquipment/Index
        public async Task<IActionResult> Index()
        {
            var equipments = await _context.Equipments
                .Include(e => e.GymEquipments)
                .OrderBy(e => e.Category)
                .ThenBy(e => e.Name)
                .ToListAsync();

            return View(equipments);
        }

        // ==================== ADM-07: THÊM THIẾT BỊ MỚI ====================
        // GET /AdminEquipment/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = EquipmentCategories;
            return View(new Equipment());
        }

        // POST /AdminEquipment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Equipment model, IFormFile? imageFile)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("GymEquipments");

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Vui lòng nhập tên thiết bị.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = EquipmentCategories;
                return View(model);
            }

            // Xử lý upload ảnh vật lý vào wwwroot/images/equipments/uploads
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("ImageUrl", "Chỉ chấp nhận file ảnh định dạng .jpg, .jpeg, .png, .webp, .svg.");
                    ViewBag.Categories = EquipmentCategories;
                    return View(model);
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "images", "equipments", "uploads");
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

                model.ImageUrl = $"/images/equipments/uploads/{newFileName}";
            }

            _context.Equipments.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm thiết bị \"{model.Name}\" vào Catalog thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ==================== ADM-08: CHỈNH SỬA THIẾT BỊ ====================
        // GET /AdminEquipment/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null) return NotFound();

            ViewBag.Categories = EquipmentCategories;
            return View(equipment);
        }

        // POST /AdminEquipment/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Equipment model, IFormFile? imageFile)
        {
            if (id != model.Id) return NotFound();

            ModelState.Remove("ImageUrl");
            ModelState.Remove("GymEquipments");

            var existing = await _context.Equipments.FindAsync(id);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Vui lòng nhập tên thiết bị.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = EquipmentCategories;
                return View(model);
            }

            // Xử lý upload ảnh mới nếu có
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("ImageUrl", "Chỉ chấp nhận file ảnh định dạng .jpg, .jpeg, .png, .webp, .svg.");
                    ViewBag.Categories = EquipmentCategories;
                    return View(model);
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "images", "equipments", "uploads");
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

                existing.ImageUrl = $"/images/equipments/uploads/{newFileName}";
            }

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Category = model.Category;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật thông tin thiết bị \"{existing.Name}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ==================== ADM-09: XÓA THIẾT BỊ ====================
        // POST /AdminEquipment/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var equipment = await _context.Equipments
                .Include(e => e.GymEquipments)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (equipment == null) return NotFound();

            // Kiểm tra ràng buộc: Nếu có phòng Gym đang dùng thiết bị này trong danh mục thì không cho xóa
            bool isUsedInGyms = equipment.GymEquipments != null && equipment.GymEquipments.Any();
            if (isUsedInGyms)
            {
                TempData["Error"] = $"Không thể xóa thiết bị \"{equipment.Name}\" vì đang có {equipment.GymEquipments.Count} phòng Gym sử dụng trong danh mục!";
                return RedirectToAction(nameof(Index));
            }

            _context.Equipments.Remove(equipment);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa thiết bị \"{equipment.Name}\" khỏi Catalog thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
