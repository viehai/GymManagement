using GymManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Quản lý trạng thái phòng Gym nâng cao cho Admin (ADM-05: Khóa/Đình chỉ Gym vi phạm).
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminGymController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminGymController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==================== ADM-05: KHÓA / GỠ ĐÌNH CHỈ 1 GYM VI PHẠM ====================
        // POST /AdminGym/Suspend/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id)
        {
            var gym = await _context.Gyms.FindAsync(id);
            if (gym == null) return NotFound();

            var currentAdmin = await _userManager.GetUserAsync(User);
            bool isReopening = gym.Status == "Suspended";

            if (isReopening)
            {
                gym.Status = "Approved";
                TempData["Success"] = $"Đã mở lại hoạt động cho phòng Gym \"{gym.Name}\".";
            }
            else
            {
                gym.Status = "Suspended";
                TempData["Warning"] = $"Đã đình chỉ hoạt động phòng Gym \"{gym.Name}\".";
            }

            _context.SystemLogs.Add(new SystemLog
            {
                UserId = currentAdmin?.Id,
                Action = isReopening ? "GymReinstated" : "GymSuspended",
                Entity = "Gym",
                EntityId = gym.Id.ToString(),
                Level = isReopening ? "Info" : "Warning",
                Description = $"Quản trị viên đã {(isReopening ? "mở lại hoạt động (Approved)" : "đình chỉ hoạt động (Suspended)")} cơ sở phòng Gym \"{gym.Name}\".",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("AllGyms", "Admin");
        }
    }
}
