using GymManagement.Helpers;
using GymManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailHelper _emailHelper;

        public AdminController(
            GymDbContext context,
            UserManager<ApplicationUser> userManager,
            EmailHelper emailHelper)
        {
            _context = context;
            _userManager = userManager;
            _emailHelper = emailHelper;
        }

        // ==================== DASHBOARD ====================
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalGyms = await _context.Gyms.CountAsync();
            ViewBag.PendingGyms = await _context.Gyms.CountAsync(g => g.Status == "Pending");
            ViewBag.ApprovedGyms = await _context.Gyms.CountAsync(g => g.Status == "Approved");
            ViewBag.RejectedGyms = await _context.Gyms.CountAsync(g => g.Status == "Rejected");
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();

            var recentGyms = await _context.Gyms
                .Include(g => g.Owner)
                .OrderByDescending(g => g.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentGyms = recentGyms;
            return View();
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
            }

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
            await _context.SaveChangesAsync();

            var owner = gym.Owner;
            if (owner != null)
            {
                string rejectSubject = "Thong bao ve don dang ky phong Gym - GymPro";
                string rejectBody = BuildRejectEmail(owner.FullName, gym.Name, reason);
                await _emailHelper.SendEmailAsync(owner.Email!, rejectSubject, rejectBody);
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
