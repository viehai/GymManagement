using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagement.Helpers;

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
                .Include(g => g.GymImages)
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
                .Include(g => g.GymImages)
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
                CreatedAt = VnTime.Now
            };

            _context.Gyms.Add(gym);
            await _context.SaveChangesAsync(); // Lưu trước để có gym.Id cho gallery

            // Xử lý GalleryFiles — upload ảnh gallery ngay sau khi tạo gym
            if (model.GalleryFiles != null && model.GalleryFiles.Any())
                await SaveGalleryFilesAsync(gym.Id, model.GalleryFiles);

            var user = await _userManager.GetUserAsync(User);
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = userId,
                Action = "GymRegistrationSubmitted",
                Entity = "Gym",
                EntityId = gym.Id.ToString(),
                Level = "Info",
                Description = $"Chủ phòng {user?.FullName} ({user?.Email}) đã tạo cơ sở phòng Gym mới \"{gym.Name}\" và đang chờ Admin duyệt.",
                CreatedAt = VnTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Phòng Gym mới đã được tạo và đang chờ phê duyệt.";
            return RedirectToAction("Index");
        }

        // ==================== EDIT ====================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms
                .Include(g => g.GymImages)
                .FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == userId);
            if (gym == null) return NotFound();

            // Nếu gym có ImageUrl từ trước nhưng bảng GymImages chưa có bản ghi nào:
            // Tự động chuyển thành ảnh đầu tiên trong GymImages (IsCover = true)
            if (!gym.GymImages.Any() && !string.IsNullOrEmpty(gym.ImageUrl))
            {
                var legacyImg = new GymImage
                {
                    GymId = gym.Id,
                    ImageUrl = gym.ImageUrl,
                    DisplayOrder = 1,
                    IsCover = true,
                    UploadedAt = gym.CreatedAt
                };
                _context.GymImages.Add(legacyImg);
                await _context.SaveChangesAsync();
                gym.GymImages.Add(legacyImg);
            }
            else if (gym.GymImages.Any())
            {
                var cover = gym.GymImages.FirstOrDefault(i => i.IsCover);
                if (cover == null)
                {
                    cover = gym.GymImages.OrderBy(i => i.DisplayOrder).First();
                    cover.IsCover = true;
                }
                if (gym.ImageUrl != cover.ImageUrl)
                {
                    gym.ImageUrl = cover.ImageUrl;
                    await _context.SaveChangesAsync();
                }
            }

            var model = new RegisterGymViewModel
            {
                Name           = gym.Name,
                Address        = gym.Address,
                Description    = gym.Description,
                ExistingImages = gym.GymImages
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new GymImageViewModel
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        DisplayOrder = i.DisplayOrder,
                        IsCover = i.IsCover
                    })
                    .ToList()
            };
            ViewBag.ExistingImage = gym.ImageUrl;
            ViewBag.GymId         = gym.Id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RegisterGymViewModel model)
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms
                .Include(g => g.GymImages)
                .FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == userId);
            if (gym == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.ExistingImages = gym.GymImages
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new GymImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, DisplayOrder = i.DisplayOrder, IsCover = i.IsCover })
                    .ToList();
                ViewBag.ExistingImage = gym.ImageUrl;
                ViewBag.GymId         = gym.Id;
                return View(model);
            }

            // Xử lý ảnh đại diện legacy (ImageFile) nếu có
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string? newImageUrl = await SaveImageAsync(model.ImageFile, model);
                if (newImageUrl == null)
                {
                    model.ExistingImages = gym.GymImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new GymImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, DisplayOrder = i.DisplayOrder, IsCover = i.IsCover })
                        .ToList();
                    ViewBag.ExistingImage = gym.ImageUrl;
                    ViewBag.GymId         = gym.Id;
                    return View(model);
                }
                if (!string.IsNullOrEmpty(gym.ImageUrl))
                    DeleteImage(gym.ImageUrl);
                gym.ImageUrl = newImageUrl;
            }

            // Xử lý upload ảnh gallery mới
            if (model.GalleryFiles != null && model.GalleryFiles.Any())
                await SaveGalleryFilesAsync(gym.Id, model.GalleryFiles);

            gym.Name        = model.Name;
            gym.Address     = model.Address;
            gym.Description = model.Description;

            // Đồng bộ gym.ImageUrl luôn theo ảnh bìa IsCover
            var coverImage = await _context.GymImages
                .FirstOrDefaultAsync(i => i.GymId == gym.Id && i.IsCover);
            if (coverImage != null)
            {
                gym.ImageUrl = coverImage.ImageUrl;
            }
            else
            {
                var firstImg = await _context.GymImages
                    .Where(i => i.GymId == gym.Id)
                    .OrderBy(i => i.DisplayOrder)
                    .FirstOrDefaultAsync();
                if (firstImg != null)
                {
                    firstImg.IsCover = true;
                    gym.ImageUrl = firstImg.ImageUrl;
                }
            }

            var user = await _userManager.GetUserAsync(User);
            _context.SystemLogs.Add(new SystemLog
            {
                UserId      = userId,
                Action      = "GymProfileUpdated",
                Entity      = "Gym",
                EntityId    = gym.Id.ToString(),
                Level       = "Info",
                Description = $"Chủ phòng {user?.FullName} đã cập nhật thông tin cơ sở phòng Gym \"{gym.Name}\".",
                CreatedAt   = VnTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thông tin phòng Gym đã được cập nhật.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // ==================== DELETE ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms
                .Include(g => g.MemberMemberships)
                .FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == userId);
            if (gym == null) return NotFound();

            // Chặn xóa nếu đã có hội viên từng đăng ký tại cơ sở này
            if (gym.MemberMemberships.Any())
            {
                TempData["Error"] = $"Không thể xóa cơ sở \"{gym.Name}\" vì đã có {gym.MemberMemberships.Count} hội viên đăng ký. Vui lòng liên hệ Admin để được hỗ trợ.";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(gym.ImageUrl))
                DeleteImage(gym.ImageUrl);

            _context.Gyms.Remove(gym);

            var user = await _userManager.GetUserAsync(User);
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = userId,
                Action = "GymDeleted",
                Entity = "Gym",
                EntityId = id.ToString(),
                Level = "Warning",
                Description = $"Chủ phòng {user?.FullName} đã xóa cơ sở phòng Gym \"{gym.Name}\".",
                CreatedAt = VnTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Phòng Gym đã được xóa.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCover(int imageId, string? returnUrl = null)
        {
            var userId = await GetCurrentUserIdAsync();
            var image = await _context.GymImages
                .Include(i => i.Gym)
                .FirstOrDefaultAsync(i => i.Id == imageId && i.Gym.OwnerId == userId);

            if (image == null) return NotFound();

            int gymId = image.GymId;

            // Reset tất cả ảnh trong gym về IsCover = false
            var allImages = await _context.GymImages
                .Where(i => i.GymId == gymId)
                .ToListAsync();

            foreach (var img in allImages)
                img.IsCover = false;

            // Set ảnh được chọn làm bìa
            image.IsCover = true;

            // Đồng bộ sang trường Gym.ImageUrl
            if (image.Gym != null)
            {
                image.Gym.ImageUrl = image.ImageUrl;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật ảnh bìa mới thành công.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Edit), new { id = gymId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, string? returnUrl = null)
        {
            var userId = await GetCurrentUserIdAsync();
            var image = await _context.GymImages
                .Include(i => i.Gym)
                .FirstOrDefaultAsync(i => i.Id == imageId && i.Gym.OwnerId == userId);

            if (image == null) return NotFound();

            int gymId = image.GymId;
            bool wasCover = image.IsCover;
            var gym = image.Gym;

            // Xóa file vật lý
            DeleteImage(image.ImageUrl);

            _context.GymImages.Remove(image);
            await _context.SaveChangesAsync();

            // Nếu ảnh bị xóa là bìa → tự động đặt ảnh đầu tiên còn lại làm bìa
            if (wasCover)
            {
                var nextCover = await _context.GymImages
                    .Where(i => i.GymId == gymId)
                    .OrderBy(i => i.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (nextCover != null)
                {
                    nextCover.IsCover = true;
                    if (gym != null)
                        gym.ImageUrl = nextCover.ImageUrl;
                }
                else
                {
                    if (gym != null)
                        gym.ImageUrl = string.Empty;
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Đã xóa ảnh thành công.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Edit), new { id = gymId });
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

        /// <summary>
        /// Helper dùng chung cho Create và Edit — upload batch GalleryFiles vào GymImages.
        /// Tự động set IsCover = true cho ảnh đầu tiên nếu gym chưa có ảnh bìa.
        /// </summary>
        private async Task SaveGalleryFilesAsync(int gymId, List<IFormFile> files)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "gyms", gymId.ToString());
            Directory.CreateDirectory(uploadFolder);

            var gym = await _context.Gyms.FindAsync(gymId);

            // Đếm ảnh hiện tại
            var currentImages = await _context.GymImages
                .Where(i => i.GymId == gymId)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync();

            int currentCount = currentImages.Count;
            int nextOrder    = currentImages.Any() ? currentImages.Max(i => i.DisplayOrder) + 1 : 1;
            bool hasAnyCover = currentImages.Any(i => i.IsCover);
            int uploadedCount = 0;
            string? firstCoverUrl = null;

            foreach (var file in files)
            {
                if (currentCount + uploadedCount >= 10) break;
                if (file == null || file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext)) continue;
                if (file.Length > 5 * 1024 * 1024) continue;

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var imgUrl = $"/uploads/gyms/{gymId}/{fileName}";
                bool isFirstCover = !hasAnyCover && uploadedCount == 0;

                _context.GymImages.Add(new GymImage
                {
                    GymId        = gymId,
                    ImageUrl     = imgUrl,
                    DisplayOrder = nextOrder++,
                    IsCover      = isFirstCover,
                    UploadedAt   = VnTime.Now
                });

                uploadedCount++;
                if (isFirstCover)
                {
                    hasAnyCover = true;
                    firstCoverUrl = imgUrl;
                }
            }

            if (uploadedCount > 0)
            {
                if (gym != null)
                {
                    if (!string.IsNullOrEmpty(firstCoverUrl))
                    {
                        gym.ImageUrl = firstCoverUrl;
                    }
                    else if (string.IsNullOrEmpty(gym.ImageUrl))
                    {
                        var existingCover = currentImages.FirstOrDefault(i => i.IsCover);
                        if (existingCover != null)
                            gym.ImageUrl = existingCover.ImageUrl;
                    }
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
