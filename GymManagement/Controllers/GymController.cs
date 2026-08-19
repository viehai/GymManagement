using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Controller xử lý trang Tìm kiếm và Chi tiết phòng Gym (công khai, không yêu cầu đăng nhập).
    /// </summary>
    public class GymController : Controller
    {
        private readonly GymDbContext _context;

        public GymController(GymDbContext context)
        {
            _context = context;
        }

        // GET: /Gym/Search?keyword=...
        /// <summary>Trang Tìm kiếm &amp; Danh sách phòng Gym đã được duyệt.</summary>
        public async Task<IActionResult> Search(string? keyword)
        {
            ViewData["Keyword"] = keyword ?? string.Empty;

            var query = _context.Gyms
                .Where(g => g.Status == "Approved")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(g =>
                    g.Name.ToLower().Contains(kw) ||
                    g.Address.ToLower().Contains(kw));
            }

            var gyms = await query
                .OrderByDescending(g => g.CreatedAt)
                .Select(g => new GymSearchViewModel
                {
                    Id          = g.Id,
                    Name        = g.Name,
                    Address     = g.Address,
                    Description = g.Description ?? string.Empty,
                    ImageUrl    = g.ImageUrl ?? string.Empty,
                    Status      = g.Status,
                    CreatedAt   = g.CreatedAt
                })
                .ToListAsync();

            return View(gyms);
        }

        // GET: /Gym/Details/{id}
        /// <summary>
        /// Trang chi tiết phòng Gym: thông tin tổng quan, thiết bị (IsVisible=true),
        /// gói vé (IsActive=true) và nút mua vé / đăng ký gói.
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            // Load gym + equipment (kèm catalog) + packages
            var gym = await _context.Gyms
                .Include(g => g.GymEquipments)
                    .ThenInclude(ge => ge.Equipment)   // catalog equipment
                .Include(g => g.MembershipPackages)
                .FirstOrDefaultAsync(g => g.Id == id && g.Status == "Approved");

            if (gym == null)
                return NotFound();

            // ── Map Equipment: chỉ lấy IsVisible = true ──
            var equipments = gym.GymEquipments
                .Where(ge => ge.IsVisible)
                .Select(ge => new GymEquipmentDisplayViewModel
                {
                    IsCustom    = ge.IsCustom,
                    DisplayName = ge.IsCustom
                        ? (ge.CustomName ?? "Máy tập")
                        : (ge.Equipment?.Name ?? "Máy tập"),
                    DisplayImage = ge.IsCustom
                        ? (ge.CustomImage ?? string.Empty)
                        : (ge.Equipment?.ImageUrl ?? string.Empty)
                })
                .ToList();

            // ── Map Packages: chỉ lấy IsActive = true, sắp xếp theo Price tăng dần ──
            var packages = gym.MembershipPackages
                .Where(p => p.IsActive)
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

            var vm = new GymDetailsViewModel
            {
                Id          = gym.Id,
                Name        = gym.Name,
                Address     = gym.Address,
                Description = gym.Description ?? string.Empty,
                ImageUrl    = gym.ImageUrl ?? string.Empty,
                Equipments  = equipments,
                Packages    = packages
            };

            return View(vm);
        }
    }
}

