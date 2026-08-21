using GymManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GymManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly GymDbContext _context;

        public HomeController(GymDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredGyms = await _context.Gyms
                .Include(g => g.MembershipPackages)
                .Where(g => g.Status == "Approved")
                .OrderByDescending(g => g.CreatedAt)
                .Take(3)
                .ToListAsync();

            return View(featuredGyms);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
