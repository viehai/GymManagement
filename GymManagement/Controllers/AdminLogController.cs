using System;
using System.Linq;
using System.Threading.Tasks;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLogController : Controller
    {
        private readonly GymDbContext _context;

        public AdminLogController(GymDbContext context)
        {
            _context = context;
        }

        // ==================== ADM-13: DANH SÁCH NHẬT KÝ HỆ THỐNG ====================
        // GET: /AdminLog/Index?level=all&actionName=...&search=...
        public async Task<IActionResult> Index(string? level = "all", string? actionName = null, string? search = null)
        {
            var rawLogs = await _context.SystemLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // Lấy danh sách Action khả dụng & Thống kê tổng quan
            var availableActions = rawLogs
                .Select(l => l.Action)
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            int totalCount = rawLogs.Count;
            int infoCount = rawLogs.Count(l => string.Equals(l.Level, "Info", StringComparison.OrdinalIgnoreCase));
            int warningCount = rawLogs.Count(l => string.Equals(l.Level, "Warning", StringComparison.OrdinalIgnoreCase));
            int errorCount = rawLogs.Count(l => string.Equals(l.Level, "Error", StringComparison.OrdinalIgnoreCase));

            // Áp dụng bộ lọc
            var query = rawLogs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(level) && level != "all")
            {
                query = query.Where(l => string.Equals(l.Level, level, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(actionName))
            {
                query = query.Where(l => string.Equals(l.Action, actionName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(l =>
                    (l.Description != null && l.Description.ToLower().Contains(kw)) ||
                    (l.Action != null && l.Action.ToLower().Contains(kw)) ||
                    (l.Entity != null && l.Entity.ToLower().Contains(kw)) ||
                    (l.EntityId != null && l.EntityId.ToLower().Contains(kw)) ||
                    (l.User != null && l.User.Email != null && l.User.Email.ToLower().Contains(kw)) ||
                    (l.User != null && l.User.FullName != null && l.User.FullName.ToLower().Contains(kw))
                );
            }

            var items = query.Select(l => new AdminLogItemViewModel
            {
                Id = l.Id,
                UserId = l.UserId,
                UserEmail = l.User?.Email ?? (l.UserId != null ? l.UserId : "Hệ thống (System)"),
                UserFullName = l.User?.FullName ?? (l.UserId != null ? "Người dùng ẩn" : "System Core"),
                Action = l.Action ?? string.Empty,
                Entity = l.Entity ?? string.Empty,
                EntityId = l.EntityId ?? string.Empty,
                Description = l.Description ?? string.Empty,
                Level = char.ToUpper(l.Level[0]) + l.Level.Substring(1).ToLower(),
                CreatedAt = l.CreatedAt
            }).ToList();

            var vm = new AdminLogListViewModel
            {
                Items = items,
                CurrentLevel = level ?? "all",
                CurrentAction = actionName,
                SearchKeyword = search,
                TotalCount = totalCount,
                InfoCount = infoCount,
                WarningCount = warningCount,
                ErrorCount = errorCount,
                AvailableActions = availableActions
            };

            return View(vm);
        }

        // ==================== ADM-14: XEM CHI TIẾT 1 LOG ENTRY ====================
        // GET: /AdminLog/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var log = await _context.SystemLogs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (log == null)
            {
                TempData["Error"] = "Không tìm thấy bản ghi nhật ký yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new AdminLogItemViewModel
            {
                Id = log.Id,
                UserId = log.UserId,
                UserEmail = log.User?.Email ?? (log.UserId != null ? log.UserId : "Hệ thống (System)"),
                UserFullName = log.User?.FullName ?? (log.UserId != null ? "Người dùng ẩn" : "System Core"),
                Action = log.Action ?? string.Empty,
                Entity = log.Entity ?? string.Empty,
                EntityId = log.EntityId ?? string.Empty,
                Description = log.Description ?? string.Empty,
                Level = char.ToUpper(log.Level[0]) + log.Level.Substring(1).ToLower(),
                CreatedAt = log.CreatedAt
            };

            return View(vm);
        }
    }
}
