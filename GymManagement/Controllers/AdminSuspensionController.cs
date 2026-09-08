using GymManagement.Helpers;
using GymManagement.Hubs;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Giám sát các quyết định đình chỉ hội viên trên toàn bộ hệ thống phòng Gym (ADM-17).
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminSuspensionController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailHelper _emailHelper;
        private readonly IHubContext<NotificationHub> _hub;

        public AdminSuspensionController(
            GymDbContext context,
            UserManager<ApplicationUser> userManager,
            EmailHelper emailHelper,
            IHubContext<NotificationHub> hub)
        {
            _context = context;
            _userManager = userManager;
            _emailHelper = emailHelper;
            _hub = hub;
        }

        // ==================== ADM-17: GIÁM SÁT ĐÌNH CHỈ TOÀN HỆ THỐNG ====================
        // GET /AdminSuspension/Index?gymId=...&status=...&search=...
        public async Task<IActionResult> Index(int? gymId, string? status, string? search)
        {
            var gyms = await _context.Gyms
                .OrderBy(g => g.Name)
                .ToListAsync();

            var query = _context.MemberSuspensions
                .Include(s => s.Gym)
                .Include(s => s.Member)
                .Include(s => s.SuspendedByUser)
                .AsQueryable();

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(s => s.GymId == gymId.Value);
            }

            var now = VnTime.Now;

            if (status == "Active")
            {
                query = query.Where(s => s.Status == "Active" && (s.SuspensionType == "Permanent" || (s.EndDate != null && s.EndDate > now)));
            }
            else if (status == "Lifted")
            {
                query = query.Where(s => s.Status == "Lifted");
            }
            else if (status == "Expired")
            {
                query = query.Where(s => s.Status == "Active" && s.SuspensionType == "Temporary" && s.EndDate != null && s.EndDate <= now);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var sTerm = search.Trim().ToLower();
                query = query.Where(s =>
                    (s.Member.FullName != null && s.Member.FullName.ToLower().Contains(sTerm)) ||
                    (s.Member.Email != null && s.Member.Email.ToLower().Contains(sTerm)) ||
                    (s.Gym.Name != null && s.Gym.Name.ToLower().Contains(sTerm)) ||
                    s.Reason.ToLower().Contains(sTerm));
            }

            var allSuspensions = await _context.MemberSuspensions.ToListAsync();
            ViewBag.TotalCount = allSuspensions.Count;
            ViewBag.ActiveCount = allSuspensions.Count(s => s.Status == "Active" && (s.SuspensionType == "Permanent" || (s.EndDate != null && s.EndDate > now)));
            ViewBag.PermanentCount = allSuspensions.Count(s => s.SuspensionType == "Permanent" && s.Status == "Active");
            ViewBag.LiftedCount = allSuspensions.Count(s => s.Status == "Lifted");

            var list = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var vmList = list.Select(s => new SuspensionItemViewModel
            {
                Id                  = s.Id,
                GymId               = s.GymId,
                GymName             = s.Gym?.Name ?? "—",
                MemberId            = s.MemberId,
                MemberName          = s.Member?.FullName ?? s.Member?.UserName ?? "—",
                MemberEmail         = s.Member?.Email ?? "—",
                SuspendedByUserId   = s.SuspendedByUserId,
                SuspendedByUserName = s.SuspendedByUser?.FullName ?? s.SuspendedByUser?.UserName ?? "Chủ phòng",
                Reason              = s.Reason,
                SuspensionType      = s.SuspensionType,
                StartDate           = s.StartDate,
                EndDate             = s.EndDate,
                Status              = s.Status,
                LiftedAt            = s.LiftedAt,
                LiftedReason        = s.LiftedReason,
                CreatedAt           = s.CreatedAt
            }).ToList();

            ViewBag.Gyms = gyms;
            ViewBag.SelectedGymId = gymId;
            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.SearchTerm = search;

            return View(vmList);
        }

        // POST /AdminSuspension/LiftSuspension
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LiftSuspension(int suspensionId, string? liftedReason)
        {
            var suspension = await _context.MemberSuspensions
                .Include(s => s.Gym)
                .Include(s => s.Member)
                .FirstOrDefaultAsync(s => s.Id == suspensionId);

            if (suspension == null) return NotFound();

            if (suspension.Status != "Active")
            {
                TempData["Error"] = "Lệnh đình chỉ này không còn ở trạng thái hiệu lực.";
                return RedirectToAction("Index");
            }

            var adminUser = await _userManager.GetUserAsync(User);
            var adminName = adminUser?.FullName ?? adminUser?.UserName ?? "Admin";

            suspension.Status       = "Lifted";
            suspension.LiftedAt     = VnTime.Now;
            suspension.LiftedReason = string.IsNullOrWhiteSpace(liftedReason)
                ? $"Quản trị viên ({adminName}) gỡ bỏ can thiệp."
                : $"[Admin can thiệp] {liftedReason.Trim()}";

            var mName = suspension.Member?.FullName ?? suspension.Member?.UserName ?? "Hội viên";

            _context.SystemLogs.Add(new SystemLog
            {
                UserId      = adminUser?.Id,
                Action      = "AdminLiftedSuspension",
                Entity      = "MemberSuspension",
                EntityId    = suspension.Id.ToString(),
                Level       = "Warning",
                Description = $"Quản trị viên {adminName} đã gỡ bỏ quyết định đình chỉ đối với hội viên {mName} tại cơ sở \"{suspension.Gym?.Name}\".",
                CreatedAt   = VnTime.Now
            });

            await _context.SaveChangesAsync();

            if (suspension.Member != null && !string.IsNullOrEmpty(suspension.Member.Email))
            {
                await _emailHelper.SendSuspensionLiftedEmailAsync(
                    suspension.Member.Email,
                    mName,
                    suspension.Gym?.Name ?? "Phòng Gym",
                    suspension.LiftedReason);
            }

            // Gửi thông báo hệ thống cho hội viên
            try
            {
                await NotificationHelper.CreateAsync(
                    _context,
                    suspension.MemberId,
                    "Đình chỉ đã được gỡ bỏ",
                    $"Quản trị viên đã gỡ bỏ lệnh đình chỉ của bạn tại cơ sở \"{suspension.Gym?.Name ?? "phòng Gym"}\". Bạn có thể tiếp tục tập luyện và mua vé.",
                    "Success",
                    "Suspension",
                    "/Member/MyMemberships",
                    _hub);
            }
            catch { /* Không ngắt luồng */ }

            TempData["Success"] = $"Quản trị viên đã gỡ bỏ lệnh đình chỉ cho hội viên {mName} thành công.";
            return RedirectToAction("Index");
        }
    }
}
