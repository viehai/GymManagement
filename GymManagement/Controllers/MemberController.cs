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

            ViewBag.Roles = roles;
            ViewBag.Gyms = myGyms;
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
    }
}
