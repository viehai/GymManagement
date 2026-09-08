using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==================== MEM-24: HỘP THƯ THÔNG BÁO ====================
        [HttpGet]
        public async Task<IActionResult> Index(string filter = "all", string? category = null, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Tự động kiểm tra và sinh thông báo vé sắp hết hạn (nếu có)
            await NotificationHelper.CheckAndGenerateMembershipExpiryNotificationsAsync(_context, user.Id);

            var query = _context.Notifications
                .Where(n => n.UserId == user.Id);

            int totalCount = await query.CountAsync();
            int unreadCount = await query.CountAsync(n => !n.IsRead);
            int readCount = totalCount - unreadCount;

            // Áp dụng bộ lọc trạng thái
            if (filter == "unread")
            {
                query = query.Where(n => !n.IsRead);
            }
            else if (filter == "read")
            {
                query = query.Where(n => n.IsRead);
            }

            // Áp dụng bộ lọc phân loại
            if (!string.IsNullOrWhiteSpace(category) && category != "all")
            {
                query = query.Where(n => n.Category == category);
            }

            int pageSize = 15;
            int totalFilteredItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalFilteredItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationItemViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Category = n.Category,
                    LinkUrl = n.LinkUrl,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            var vm = new NotificationIndexViewModel
            {
                Notifications = items,
                CurrentFilter = filter,
                CurrentCategory = category,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                ReadCount = readCount,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(vm);
        }

        // ==================== LẤY SỐ ĐẾM CHƯA ĐỌC (AJAX) ====================
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { unreadCount = 0 });

            // Tự động kiểm tra vé sắp hết hạn
            await NotificationHelper.CheckAndGenerateMembershipExpiryNotificationsAsync(_context, user.Id);

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == user.Id && !n.IsRead);

            return Json(new { unreadCount });
        }

        // ==================== LẤY 5 THÔNG BÁO MỚI NHẤT CHO DROPDOWN (AJAX) ====================
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { unreadCount = 0, items = new List<object>() });

            // Tự động kiểm tra vé sắp hết hạn
            await NotificationHelper.CheckAndGenerateMembershipExpiryNotificationsAsync(_context, user.Id);

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == user.Id && !n.IsRead);

            var recent = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    category = n.Category,
                    categoryName = NotificationHelper.GetCategoryDisplayName(n.Category),
                    categoryIcon = NotificationHelper.GetCategoryIcon(n.Category, n.Type),
                    linkUrl = n.LinkUrl,
                    isRead = n.IsRead,
                    timeAgo = NotificationHelper.GetTimeAgo(n.CreatedAt)
                })
                .ToListAsync();

            return Json(new { unreadCount, items = recent });
        }

        // ==================== MEM-25: ĐÁNH DẤU 1 THÔNG BÁO LÀ ĐÃ ĐỌC (AJAX/POST) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                int remainingUnread = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
                return Json(new { success = true, remainingUnread });
            }

            return RedirectToAction(nameof(Index));
        }

        // ==================== MEM-25: ĐÁNH DẤU TẤT CẢ LÀ ĐÃ ĐỌC ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var unreadList = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            if (unreadList.Any())
            {
                foreach (var item in unreadList)
                {
                    item.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, unreadCount = 0 });
            }

            TempData["Success"] = "Đã đánh dấu tất cả thông báo là đã đọc.";
            return RedirectToAction(nameof(Index));
        }

        // ==================== XÓA 1 THÔNG BÁO ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                int remainingUnread = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
                return Json(new { success = true, remainingUnread });
            }

            TempData["Success"] = "Đã xóa thông báo.";
            return RedirectToAction(nameof(Index));
        }

        // ==================== DỌN DẸP TẤT CẢ THÔNG BÁO ĐÃ ĐỌC ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var readList = await _context.Notifications
                .Where(n => n.UserId == user.Id && n.IsRead)
                .ToListAsync();

            if (readList.Any())
            {
                _context.Notifications.RemoveRange(readList);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã dọn dẹp {readList.Count} thông báo đã đọc.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
