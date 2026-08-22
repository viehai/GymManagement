using GymManagement.Models;
using Microsoft.AspNetCore.Authorization;
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

        public AdminGymController(GymDbContext context)
        {
            _context = context;
        }

        // ==================== ADM-05: KHÓA / GỠ ĐÌNH CHỈ 1 GYM VI PHẠM ====================
        // POST /AdminGym/Suspend/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id)
        {
            var gym = await _context.Gyms.FindAsync(id);
            if (gym == null) return NotFound();

            if (gym.Status == "Suspended")
            {
                gym.Status = "Approved";
                TempData["Success"] = $"Đã mở lại hoạt động cho phòng Gym \"{gym.Name}\".";
            }
            else
            {
                gym.Status = "Suspended";
                TempData["Warning"] = $"Đã đình chỉ hoạt động phòng Gym \"{gym.Name}\".";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("AllGyms", "Admin");
        }
    }
}
