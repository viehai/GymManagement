using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Quản lý người dùng, duyệt Owner và phân quyền hệ thống (ADM-01, ADM-02, ADM-10, ADM-11, ADM-12).
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminUserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly GymDbContext _context;
        private readonly EmailHelper _emailHelper;

        public AdminUserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            GymDbContext context,
            EmailHelper emailHelper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailHelper = emailHelper;
        }

        // ==================== ADM-10 & ADM-01: DANH SÁCH NGƯỜI DÙNG ====================
        // GET /AdminUser/Index?filter=all&search=...
        public async Task<IActionResult> Index(string filter = "all", string? search = null)
        {
            // 1. Lấy toàn bộ User kèm thông tin Gyms
            var users = await _context.Users
                .Include(u => u.Gyms)
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            // 2. Lấy mapping User - Role tối ưu bằng 1 query
            var userRoles = await (from ur in _context.UserRoles
                                   join r in _context.Roles on ur.RoleId equals r.Id
                                   select new { ur.UserId, RoleName = r.Name })
                                   .ToListAsync();

            var roleDict = userRoles
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.RoleName)
                          .OrderByDescending(r => r == "Admin" ? 3 : r == "Owner" ? 2 : 1)
                          .FirstOrDefault() ?? "Member");

            // 3. Map sang List ViewModel
            var allItems = users.Select(u =>
            {
                var isLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow;
                var pendingGym = u.Gyms?.FirstOrDefault(g => g.Status == "Pending");
                var role = roleDict.TryGetValue(u.Id, out var rName) ? rName : "Member";

                return new AdminUserItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName ?? "Chưa đặt tên",
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber ?? "—",
                    Role = role,
                    IsLocked = isLocked,
                    HasPendingGym = pendingGym != null,
                    PendingGymName = pendingGym?.Name ?? string.Empty,
                    GymCount = u.Gyms?.Count ?? 0
                };
            }).ToList();

            // 4. Tính toán số lượng cho bộ đếm
            var totalCount = allItems.Count;
            var memberCount = allItems.Count(u => u.Role == "Member");
            var ownerCount = allItems.Count(u => u.Role == "Owner");
            var pendingCount = allItems.Count(u => u.HasPendingGym);
            var adminCount = allItems.Count(u => u.Role == "Admin");
            var lockedCount = allItems.Count(u => u.IsLocked);

            // 5. Lọc theo tham số filter
            var filtered = filter.ToLower() switch
            {
                "member" => allItems.Where(u => u.Role == "Member"),
                "owner" => allItems.Where(u => u.Role == "Owner"),
                "pending" => allItems.Where(u => u.HasPendingGym),
                "admin" => allItems.Where(u => u.Role == "Admin"),
                "locked" => allItems.Where(u => u.IsLocked),
                _ => allItems.AsEnumerable()
            };

            // 6. Lọc theo từ khóa tìm kiếm (nếu có)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                filtered = filtered.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s) ||
                    u.PhoneNumber.Contains(s) ||
                    u.PendingGymName.ToLower().Contains(s));
            }

            var vm = new AdminUserListViewModel
            {
                Users = filtered.ToList(),
                CurrentFilter = filter.ToLower(),
                SearchQuery = search,
                TotalCount = totalCount,
                MemberCount = memberCount,
                OwnerCount = ownerCount,
                PendingCount = pendingCount,
                AdminCount = adminCount,
                LockedCount = lockedCount
            };

            return View(vm);
        }

        // ==================== ADM-11 & ADM-02: KHÓA / MỞ KHÓA TÀI KHOẢN ====================
        // POST /AdminUser/ToggleLock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id, string? returnFilter = "all")
        {
            var currentAdmin = await _userManager.GetUserAsync(User);
            if (currentAdmin != null && currentAdmin.Id == id)
            {
                TempData["Error"] = "Không thể tự khóa tài khoản Admin của chính mình.";
                return RedirectToAction(nameof(Index), new { filter = returnFilter });
            }

            var targetUser = await _userManager.FindByIdAsync(id);
            if (targetUser == null) return NotFound();

            var isCurrentlyLocked = targetUser.LockoutEnd.HasValue && targetUser.LockoutEnd.Value > DateTimeOffset.UtcNow;

            if (isCurrentlyLocked)
            {
                // Mở khóa
                await _userManager.SetLockoutEndDateAsync(targetUser, null);
                TempData["Success"] = $"Đã mở khóa tài khoản \"{targetUser.Email}\".";
            }
            else
            {
                // Khóa tài khoản
                await _userManager.SetLockoutEndDateAsync(targetUser, DateTimeOffset.UtcNow.AddYears(100));
                await _userManager.UpdateSecurityStampAsync(targetUser);
                TempData["Warning"] = $"Đã khóa tài khoản \"{targetUser.Email}\".";
            }

            return RedirectToAction(nameof(Index), new { filter = returnFilter });
        }

        // ==================== ADM-02: DUYỆT OWNER & PHÒNG GYM CHỜ ====================
        // POST /AdminUser/ApproveOwner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOwner(string id, string? returnFilter = "pending")
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // 1. Gán vai trò Owner nếu chưa có
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Owner"))
            {
                await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRoleAsync(user, "Owner");
                await _userManager.UpdateSecurityStampAsync(user);
            }

            // 2. Duyệt tất cả phòng Gym Pending của user này
            var pendingGyms = await _context.Gyms
                .Where(g => g.OwnerId == user.Id && g.Status == "Pending")
                .ToListAsync();

            foreach (var gym in pendingGyms)
            {
                gym.Status = "Approved";

                // Gửi email thông báo
                string approveSubject = "Phòng Gym của bạn đã được phê duyệt! - GymPro";
                string approveBody =
                    "<div style='font-family:Arial,sans-serif;padding:20px;border:1px solid #ddd;'>" +
                    "<h2 style='color:#000;'>Chúc mừng, " + user.FullName + "!</h2>" +
                    "<p>Phòng Gym <strong>" + gym.Name + "</strong> (" + gym.Address + ") đã được phê duyệt thành công.</p>" +
                    "<p>Tài khoản của bạn đã được cấp quyền <strong>Chủ phòng Gym (Owner)</strong>. Vui lòng đăng nhập lại để bắt đầu quản lý.</p>" +
                    "</div>";

                await _emailHelper.SendEmailAsync(user.Email!, approveSubject, approveBody);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã duyệt tài khoản \"{user.FullName}\" ({user.Email}) thành Chủ phòng Gym thành công!";
            return RedirectToAction(nameof(Index), new { filter = returnFilter });
        }

        // ==================== ADM-12: PHÂN QUYỀN THỦ CÔNG (GET) ====================
        // GET /AdminUser/EditRole/{id}
        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? "Member";

            var vm = new AdminEditRoleViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                CurrentRole = currentRole,
                SelectedRole = currentRole
            };

            return View(vm);
        }

        // ==================== ADM-12: PHÂN QUYỀN THỦ CÔNG (POST) ====================
        // POST /AdminUser/EditRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(AdminEditRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // Ngăn chặn admin tự hạ quyền của chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id && model.SelectedRole != "Admin")
            {
                TempData["Error"] = "Không thể tự hạ quyền Administrator của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            // Cập nhật role
            var currentRoles = await _userManager.GetRolesAsync(user);
            bool wasOwner = currentRoles.Contains("Owner");
            bool willBeMember = model.SelectedRole == "Member";
            bool willBeOwner = model.SelectedRole == "Owner";

            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await _userManager.AddToRoleAsync(user, model.SelectedRole);
            await _userManager.UpdateSecurityStampAsync(user);

            string extraNotice = "";

            // LOGIC TỰ ĐỘNG: Nếu hạ quyền Owner -> Member, chuyển toàn bộ phòng Gym của người này sang 'Suspended' (Đình chỉ)
            if (wasOwner && willBeMember)
            {
                var activeGyms = await _context.Gyms
                    .Where(g => g.OwnerId == user.Id && g.Status == "Approved")
                    .ToListAsync();

                if (activeGyms.Any())
                {
                    foreach (var gym in activeGyms)
                    {
                        gym.Status = "Suspended";
                    }
                    await _context.SaveChangesAsync();
                    extraNotice = $" Đã tự động đình chỉ (Suspended) {activeGyms.Count} cơ sở phòng Gym của người này để đảm bảo an toàn.";
                }
            }
            // Nếu nâng cấp từ Member -> Owner và có phòng Gym đang Suspended, tự động mở lại Approved
            else if (!wasOwner && willBeOwner)
            {
                var suspendedGyms = await _context.Gyms
                    .Where(g => g.OwnerId == user.Id && (g.Status == "Suspended" || g.Status == "Pending"))
                    .ToListAsync();

                if (suspendedGyms.Any())
                {
                    foreach (var gym in suspendedGyms)
                    {
                        gym.Status = "Approved";
                    }
                    await _context.SaveChangesAsync();
                    extraNotice = $" Đã tự động kích hoạt lại (Approved) {suspendedGyms.Count} cơ sở phòng Gym của người này.";
                }
            }

            TempData["Success"] = $"Đã cập nhật vai trò của \"{user.Email}\" thành {model.SelectedRole} thành công!{extraNotice}";
            return RedirectToAction(nameof(Index));
        }
    }
}
