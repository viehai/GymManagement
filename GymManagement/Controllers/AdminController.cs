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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailHelper _emailHelper;
        private readonly IHubContext<NotificationHub> _hub;

        public AdminController(
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

        // ==================== DASHBOARD TOÀN SÀN ====================
        public async Task<IActionResult> Dashboard()
        {
            var now = VnTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);
            var endOfLastMonth   = startOfThisMonth.AddTicks(-1);

            // 1. Thống kê Gym
            int totalGyms    = await _context.Gyms.CountAsync();
            int pendingGyms  = await _context.Gyms.CountAsync(g => g.Status == "Pending");
            int approvedGyms = await _context.Gyms.CountAsync(g => g.Status == "Approved");
            int rejectedGyms = await _context.Gyms.CountAsync(g => g.Status == "Rejected");

            // 2. Thống kê User
            int totalUsers   = await _userManager.Users.CountAsync();
            int totalOwners  = await _context.Gyms.Select(g => g.OwnerId).Distinct().CountAsync();
            int totalMembers = Math.Max(0, totalUsers - totalOwners - 1); // Trừ owner và admin

            // 3. Tài chính toàn sàn
            var successTxs = await _context.Transactions
                .Where(t => t.Status == "Success")
                .Select(t => new { t.Amount, t.CreatedAt, GymId = t.Membership != null ? t.Membership.GymId : 0 })
                .ToListAsync();

            decimal totalGMV = successTxs.Sum(t => t.Amount);
            decimal thisMonthGMV = successTxs.Where(t => t.CreatedAt >= startOfThisMonth).Sum(t => t.Amount);
            decimal lastMonthGMV = successTxs.Where(t => t.CreatedAt >= startOfLastMonth && t.CreatedAt <= endOfLastMonth).Sum(t => t.Amount);

            double growthMoM = 0.0;
            if (lastMonthGMV > 0)
            {
                growthMoM = (double)((thisMonthGMV - lastMonthGMV) / lastMonthGMV) * 100.0;
            }
            else if (thisMonthGMV > 0)
            {
                growthMoM = 100.0;
            }

            // 4. VIP toàn sàn
            var vipStatuses = await _context.MemberVipStatuses
                .Include(v => v.CurrentTier)
                .ToListAsync();
            int silverCount = vipStatuses.Count(v => v.CurrentTier?.TierName?.ToLower().Contains("silver") == true || v.CurrentTier?.TierName?.ToLower().Contains("bạc") == true);
            int goldCount = vipStatuses.Count(v => v.CurrentTier?.TierName?.ToLower().Contains("gold") == true || v.CurrentTier?.TierName?.ToLower().Contains("vàng") == true);
            int platinumCount = vipStatuses.Count(v => v.CurrentTier?.TierName?.ToLower().Contains("platinum") == true || v.CurrentTier?.TierName?.ToLower().Contains("bạch kim") == true || v.CurrentTier?.TierName?.ToLower().Contains("kim cương") == true);
            int totalVip = vipStatuses.Count(v => v.CurrentTier != null);

            // 5. Kỷ luật & Đình chỉ
            int activeSuspensions = await _context.MemberSuspensions.CountAsync(s => s.Status == "Active");
            int liftedSuspensions = await _context.MemberSuspensions.CountAsync(s => s.Status == "Lifted");

            // 6. Biểu đồ Doanh thu toàn sàn 6 tháng gần nhất
            var monthlyChart = new List<MonthlyRevenueItem>();
            for (int i = 5; i >= 0; i--)
            {
                var mStart = startOfThisMonth.AddMonths(-i);
                var mEnd   = mStart.AddMonths(1).AddTicks(-1);

                var monthTxs = successTxs.Where(t => t.CreatedAt >= mStart && t.CreatedAt <= mEnd).ToList();
                monthlyChart.Add(new MonthlyRevenueItem
                {
                    MonthLabel       = $"T{mStart.Month}/{mStart.Year}",
                    Revenue          = monthTxs.Sum(t => t.Amount),
                    TransactionCount = monthTxs.Count
                });
            }

            // 7. Top 5 Phòng Gym doanh thu cao nhất
            var gyms = await _context.Gyms
                .Include(g => g.Owner)
                .Include(g => g.MembershipPackages)
                .Include(g => g.MemberMemberships)
                .ToListAsync();

            var topGymsByRevenue = gyms
                .Select(g =>
                {
                    decimal rev = successTxs.Where(t => t.GymId == g.Id).Sum(t => t.Amount);
                    int members = g.MemberMemberships?.Select(m => m.MemberId).Distinct().Count() ?? 0;
                    return new AdminTopGymItemViewModel
                    {
                        GymId         = g.Id,
                        GymName       = g.Name,
                        OwnerName     = g.Owner?.FullName ?? g.Owner?.UserName ?? "—",
                        Address       = g.Address,
                        TotalRevenue  = rev,
                        TotalMembers  = members,
                        TotalPackages = g.MembershipPackages?.Count ?? 0
                    };
                })
                .OrderByDescending(g => g.TotalRevenue)
                .Take(5)
                .ToList();

            // 8. Top 5 Cơ sở có nhiều ca đình chỉ nhất (Cảnh báo rủi ro)
            var suspensions = await _context.MemberSuspensions.ToListAsync();
            var topSuspensionGyms = suspensions
                .GroupBy(s => s.GymId)
                .Select(grp =>
                {
                    var gymObj = gyms.FirstOrDefault(g => g.Id == grp.Key);
                    return new AdminGymSuspensionItemViewModel
                    {
                        GymId                  = grp.Key,
                        GymName                = gymObj?.Name ?? $"Gym #{grp.Key}",
                        ActiveSuspensionsCount = grp.Count(s => s.Status == "Active"),
                        TotalSuspensionsCount  = grp.Count()
                    };
                })
                .OrderByDescending(g => g.ActiveSuspensionsCount)
                .ThenByDescending(g => g.TotalSuspensionsCount)
                .Take(5)
                .ToList();

            // 9. Cơ sở đăng ký gần nhất
            var recentGyms = gyms.OrderByDescending(g => g.CreatedAt).Take(5).ToList();

            // Đồng bộ ViewBag cho layout/sidebar
            ViewBag.TotalGyms    = totalGyms;
            ViewBag.PendingGyms  = pendingGyms;
            ViewBag.ApprovedGyms = approvedGyms;
            ViewBag.RejectedGyms = rejectedGyms;
            ViewBag.TotalUsers   = totalUsers;
            ViewBag.RecentGyms   = recentGyms;
            ViewBag.PendingBadge = pendingGyms;

            var vm = new AdminDashboardViewModel
            {
                TotalGMV                 = totalGMV,
                ThisMonthGMV             = thisMonthGMV,
                LastMonthGMV             = lastMonthGMV,
                GrowthRateMoM            = Math.Round(growthMoM, 1),
                TotalTransactions        = successTxs.Count,
                TotalGyms                = totalGyms,
                ApprovedGyms             = approvedGyms,
                PendingGyms              = pendingGyms,
                RejectedGyms             = rejectedGyms,
                TotalUsers               = totalUsers,
                TotalMembers             = totalMembers,
                TotalOwners              = totalOwners,
                TotalVipMembers          = totalVip,
                SilverVipCount           = silverCount,
                GoldVipCount             = goldCount,
                PlatinumVipCount         = platinumCount,
                TotalActiveSuspensions   = activeSuspensions,
                TotalLiftedSuspensions   = liftedSuspensions,
                MonthlyRevenueChart      = monthlyChart,
                TopGymsByRevenue         = topGymsByRevenue,
                TopGymsBySuspensions     = topSuspensionGyms,
                RecentGyms               = recentGyms
            };

            return View(vm);
        }

        // ==================== XUẤT EXCEL BÁO CÁO HỆ THỐNG TOÀN DIỆN ====================
        [HttpGet]
        public async Task<IActionResult> ExportSystemReportExcel()
        {
            var now = VnTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);

            var successTxs = await _context.Transactions
                .Where(t => t.Status == "Success")
                .Select(t => new { t.Amount, t.CreatedAt, GymId = t.Membership != null ? t.Membership.GymId : 0 })
                .ToListAsync();

            var gyms = await _context.Gyms
                .Include(g => g.Owner)
                .Include(g => g.MembershipPackages)
                .ToListAsync();

            var users = await _userManager.Users.ToListAsync();
            int totalOwners = gyms.Select(g => g.OwnerId).Distinct().Count();
            int totalMembers = Math.Max(0, users.Count - totalOwners - 1);

            var suspensions = await _context.MemberSuspensions
                .Include(s => s.Member)
                .Include(s => s.Gym)
                .ToListAsync();

            var exportData = new AdminSystemExportData
            {
                TotalGMV                = successTxs.Sum(t => t.Amount),
                ThisMonthGMV            = successTxs.Where(t => t.CreatedAt >= startOfThisMonth).Sum(t => t.Amount),
                TotalTransactions       = successTxs.Count,
                TotalGyms               = gyms.Count,
                ApprovedGyms            = gyms.Count(g => g.Status == "Approved"),
                PendingGyms             = gyms.Count(g => g.Status == "Pending"),
                RejectedGyms            = gyms.Count(g => g.Status == "Rejected"),
                TotalUsers              = users.Count,
                TotalMembers            = totalMembers,
                TotalOwners             = totalOwners,
                TotalVipMembers         = await _context.MemberVipStatuses.CountAsync(),
                TotalActiveSuspensions  = suspensions.Count(s => s.Status == "Active"),
                Gyms = gyms.Select(g => new AdminGymExportRow
                {
                    Id            = g.Id,
                    Name          = g.Name,
                    OwnerName     = g.Owner?.FullName ?? g.Owner?.UserName ?? "—",
                    OwnerEmail    = g.Owner?.Email ?? "—",
                    Address       = g.Address,
                    Status        = g.Status,
                    CreatedAt     = g.CreatedAt,
                    TotalRevenue  = successTxs.Where(t => t.GymId == g.Id).Sum(t => t.Amount),
                    TotalPackages = g.MembershipPackages?.Count ?? 0
                }).ToList(),
                Suspensions = suspensions.Select(s => new AdminSuspensionExportRow
                {
                    Id             = s.Id,
                    MemberName     = s.Member?.FullName ?? s.Member?.UserName ?? "—",
                    MemberEmail    = s.Member?.Email ?? "—",
                    GymName        = s.Gym?.Name ?? "—",
                    SuspensionType = s.SuspensionType,
                    StartDate      = s.StartDate,
                    EndDate        = s.EndDate,
                    Reason         = s.Reason,
                    Status         = s.Status
                }).ToList()
            };

            byte[] fileBytes = ExcelExportHelper.ExportAdminSystemReport(exportData);
            string fileName = $"BaoCaoHeThong_GymPro_{VnTime.Now:yyyyMMdd_HHmm}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ==================== DANH SÁCH PHÒNG GYM CHỜ DUYỆT ====================
        public async Task<IActionResult> PendingGyms()
        {
            // Truyền số lượng pending vào sidebar badge
            ViewBag.PendingBadge = await _context.Gyms.CountAsync(g => g.Status == "Pending");

            var gyms = await _context.Gyms
                .Include(g => g.Owner)
                .Where(g => g.Status == "Pending")
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(gyms);
        }

        // ==================== PHÊ DUYỆT GYM ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveGym(int id)
        {
            var gym = await _context.Gyms
                .Include(g => g.Owner)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gym == null)
                return NotFound();

            // 1. Cập nhật trạng thái Gym
            gym.Status = "Approved";
            await _context.SaveChangesAsync();

            // 2. Nâng cấp Role từ Member → Owner (nếu chưa có role Owner)
            var owner = gym.Owner;
            if (owner != null)
            {
                bool isAlreadyOwner = await _userManager.IsInRoleAsync(owner, "Owner");
                if (!isAlreadyOwner)
                {
                    if (await _userManager.IsInRoleAsync(owner, "Member"))
                    {
                        await _userManager.RemoveFromRoleAsync(owner, "Member");
                    }
                    await _userManager.AddToRoleAsync(owner, "Owner");
                    await _userManager.UpdateSecurityStampAsync(owner);
                }

                // 3. Gửi email thông báo phê duyệt
                string approveSubject = "Phong Gym cua ban da duoc phe duyet! - GymPro";
                string approveBody = BuildApproveEmail(owner.FullName, gym.Name, gym.Address);
                await _emailHelper.SendEmailAsync(owner.Email!, approveSubject, approveBody);

                // Gửi thông báo hệ thống cho Chủ phòng
                try
                {
                    await NotificationHelper.CreateAsync(
                        _context,
                        owner.Id,
                        "Phòng Gym đã được phê duyệt!",
                        $"Cơ sở phòng Gym \"{gym.Name}\" ({gym.Address}) của bạn đã được quản trị viên phê duyệt chính thức.",
                        "Success",
                        "GymApproval",
                        $"/OwnerGym/Details/{gym.Id}",
                        _hub);
                }
                catch { /* Không ngắt luồng */ }
            }

            var currentAdmin = await _userManager.GetUserAsync(User);
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = currentAdmin?.Id,
                Action = "GymApproved",
                Entity = "Gym",
                EntityId = gym.Id.ToString(),
                Level = "Info",
                Description = $"Quản trị viên đã phê duyệt cơ sở phòng Gym \"{gym.Name}\" ({gym.Address}) của chủ phòng {gym.Owner?.FullName}.",
                CreatedAt = VnTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Phong Gym da duoc phe duyet thanh cong.";
            return RedirectToAction("PendingGyms");
        }

        // ==================== TỪ CHỐI GYM ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectGym(int id, string? reason)
        {
            var gym = await _context.Gyms
                .Include(g => g.Owner)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gym == null)
                return NotFound();

            gym.Status = "Rejected";

            var currentAdmin = await _userManager.GetUserAsync(User);
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = currentAdmin?.Id,
                Action = "GymRejected",
                Entity = "Gym",
                EntityId = gym.Id.ToString(),
                Level = "Warning",
                Description = $"Quản trị viên đã từ chối đơn đăng ký phòng Gym \"{gym.Name}\". Lý do: {(string.IsNullOrWhiteSpace(reason) ? "Không đạt yêu cầu" : reason)}.",
                CreatedAt = VnTime.Now
            });
            await _context.SaveChangesAsync();

            var owner = gym.Owner;
            if (owner != null)
            {
                string rejectSubject = "Thong bao ve don dang ky phong Gym - GymPro";
                string rejectBody = BuildRejectEmail(owner.FullName, gym.Name, reason);
                await _emailHelper.SendEmailAsync(owner.Email!, rejectSubject, rejectBody);

                // Gửi thông báo hệ thống cho Chủ phòng
                try
                {
                    await NotificationHelper.CreateAsync(
                        _context,
                        owner.Id,
                        "Yêu cầu phòng Gym bị từ chối",
                        $"Cơ sở phòng Gym \"{gym.Name}\" của bạn đã bị từ chối phê duyệt. Lý do: {(string.IsNullOrWhiteSpace(reason) ? "Không đạt yêu cầu" : reason)}.",
                        "Danger",
                        "GymApproval",
                        "/OwnerGym/Index",
                        _hub);
                }
                catch { /* Không ngắt luồng */ }
            }

            TempData["Warning"] = "Da tu choi phong Gym.";
            return RedirectToAction("PendingGyms");
        }

        // ==================== DANH SÁCH TẤT CẢ GYM ====================
        public async Task<IActionResult> AllGyms(string? status)
        {
            var query = _context.Gyms.Include(g => g.Owner).AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(g => g.Status == status);

            var gyms = await query.OrderByDescending(g => g.CreatedAt).ToListAsync();
            ViewBag.CurrentStatus = status;
            return View(gyms);
        }

        // ==================== EMAIL BUILDERS ====================
        private static string BuildApproveEmail(string ownerName, string gymName, string gymAddress)
        {
            return
                "<div style='font-family:Inter,Arial,sans-serif;max-width:560px;margin:auto;border:1px solid #e8e8e8;border-radius:8px;overflow:hidden;'>" +
                    "<div style='background:#000;padding:28px 32px;'>" +
                        "<h1 style='color:#fff;font-size:1.4rem;margin:0;letter-spacing:2px;'>GYMPRO</h1>" +
                    "</div>" +
                    "<div style='padding:32px;'>" +
                        "<h2 style='color:#111;'>Chuc mung, " + ownerName + "!</h2>" +
                        "<p style='color:#444;line-height:1.6;'>" +
                            "Phong Gym <strong>" + gymName + "</strong> cua ban da duoc <strong>phe duyet thanh cong</strong> boi doi ngu GymPro." +
                        "</p>" +
                        "<p style='color:#444;line-height:1.6;'>" +
                            "Tai khoan cua ban da duoc nang cap len quyen <strong>Owner</strong>. " +
                            "Vui long <strong>dang xuat va dang nhap lai</strong> de truy cap vao bang dieu khien quan ly phong Gym." +
                        "</p>" +
                        "<div style='background:#f8f9fa;border-left:4px solid #000;padding:16px;border-radius:4px;margin:20px 0;'>" +
                            "<p style='margin:0;color:#333;font-weight:600;'>" + gymName + "</p>" +
                            "<p style='margin:4px 0 0;color:#666;font-size:0.9rem;'>" + gymAddress + "</p>" +
                        "</div>" +
                        "<p style='color:#888;font-size:0.85rem;margin-top:28px;'>Neu ban co bat ky cau hoi nao, hay lien he voi chung toi qua email ho tro.</p>" +
                    "</div>" +
                    "<div style='background:#f5f5f5;padding:16px 32px;text-align:center;'>" +
                        "<p style='margin:0;color:#999;font-size:0.75rem;'>&copy; " + DateTime.Now.Year + " GymPro Management. All rights reserved.</p>" +
                    "</div>" +
                "</div>";
        }

        private static string BuildRejectEmail(string ownerName, string gymName, string? reason)
        {
            string reasonSection = string.IsNullOrWhiteSpace(reason)
                ? ""
                : "<p style='color:#444;line-height:1.6;'><strong>Ly do:</strong> " + reason + "</p>";

            return
                "<div style='font-family:Inter,Arial,sans-serif;max-width:560px;margin:auto;border:1px solid #e8e8e8;border-radius:8px;overflow:hidden;'>" +
                    "<div style='background:#000;padding:28px 32px;'>" +
                        "<h1 style='color:#fff;font-size:1.4rem;margin:0;letter-spacing:2px;'>GYMPRO</h1>" +
                    "</div>" +
                    "<div style='padding:32px;'>" +
                        "<h2 style='color:#111;'>Xin chao " + ownerName + ",</h2>" +
                        "<p style='color:#444;line-height:1.6;'>" +
                            "Rat tiec, don dang ky phong Gym <strong>" + gymName + "</strong> cua ban chua duoc phe duyet trong lan nay." +
                        "</p>" +
                        reasonSection +
                        "<p style='color:#444;line-height:1.6;'>" +
                            "Ban co the chinh sua thong tin va gui lai don dang ky moi. Doi ngu GymPro luon san sang ho tro ban." +
                        "</p>" +
                    "</div>" +
                    "<div style='background:#f5f5f5;padding:16px 32px;text-align:center;'>" +
                        "<p style='margin:0;color:#999;font-size:0.75rem;'>&copy; " + DateTime.Now.Year + " GymPro Management. All rights reserved.</p>" +
                    "</div>" +
                "</div>";
        }
    }
}
