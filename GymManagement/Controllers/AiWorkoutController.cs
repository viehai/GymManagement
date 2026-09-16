using GymManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Member")]
    public class AiWorkoutController : Controller
    {
        private readonly GymDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        // Calo tiêu thụ ước tính mỗi rep (MET-based, trọng lượng trung bình 65kg)
        private const double CalPerSquatRep = 0.32;
        private const double CalPerPushUpRep = 0.29;

        public AiWorkoutController(GymDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ────────────────────────────────────────────────────────────────
        // GET /AiWorkout  —  Trang chọn bài tập & kỷ lục cá nhân
        // ────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Lấy 5 buổi tập gần nhất cho mỗi bài tập
            var recentSquat = await _db.WorkoutSessions
                .Where(w => w.MemberId == userId && w.ExerciseType == "Squat")
                .OrderByDescending(w => w.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentPushUp = await _db.WorkoutSessions
                .Where(w => w.MemberId == userId && w.ExerciseType == "PushUp")
                .OrderByDescending(w => w.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Kỷ lục cá nhân
            var bestSquat = await _db.WorkoutSessions
                .Where(w => w.MemberId == userId && w.ExerciseType == "Squat")
                .OrderByDescending(w => w.ValidReps)
                .FirstOrDefaultAsync();

            var bestPushUp = await _db.WorkoutSessions
                .Where(w => w.MemberId == userId && w.ExerciseType == "PushUp")
                .OrderByDescending(w => w.ValidReps)
                .FirstOrDefaultAsync();

            ViewBag.RecentSquat = recentSquat;
            ViewBag.RecentPushUp = recentPushUp;
            ViewBag.BestSquat = bestSquat;
            ViewBag.BestPushUp = bestPushUp;

            return View();
        }

        // ────────────────────────────────────────────────────────────────
        // GET /AiWorkout/Studio?exercise=Squat  —  Phòng tập AI Split-Screen
        // ────────────────────────────────────────────────────────────────
        public IActionResult Studio(string exercise = "Squat")
        {
            if (exercise != "Squat" && exercise != "PushUp")
                return RedirectToAction(nameof(Index));

            ViewBag.Exercise = exercise;
            return View();
        }

        // ────────────────────────────────────────────────────────────────
        // POST /AiWorkout/SaveSession  —  Lưu kết quả buổi tập từ client JS
        // ────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSession(
            [FromForm] string exerciseType,
            [FromForm] int totalReps,
            [FromForm] int validReps,
            [FromForm] int invalidReps,
            [FromForm] double averageFormScore,
            [FromForm] int durationSeconds)
        {
            if (exerciseType != "Squat" && exerciseType != "PushUp")
                return Json(new { success = false, message = "Bài tập không hợp lệ." });

            var userId = _userManager.GetUserId(User);

            // Tìm GymId của hội viên (ưu tiên membership đang active)
            var activeMembership = await _db.MemberMemberships
                .Where(m => m.MemberId == userId && m.EndDate >= DateTime.UtcNow)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            double caloriesPerRep = exerciseType == "Squat" ? CalPerSquatRep : CalPerPushUpRep;

            var session = new WorkoutSession
            {
                MemberId      = userId,
                GymId         = activeMembership?.GymId,
                ExerciseType  = exerciseType,
                TotalReps     = Math.Max(0, totalReps),
                ValidReps     = Math.Max(0, validReps),
                InvalidReps   = Math.Max(0, invalidReps),
                AverageFormScore = Math.Clamp(averageFormScore, 0, 100),
                DurationSeconds  = Math.Max(0, durationSeconds),
                CaloriesBurned   = Math.Round(validReps * caloriesPerRep, 2),
                CreatedAt     = DateTime.UtcNow
            };

            _db.WorkoutSessions.Add(session);
            await _db.SaveChangesAsync();

            // Kiểm tra kỷ lục cá nhân mới
            var prevBest = await _db.WorkoutSessions
                .Where(w => w.MemberId == userId && w.ExerciseType == exerciseType && w.Id != session.Id)
                .OrderByDescending(w => w.ValidReps)
                .FirstOrDefaultAsync();

            bool isPersonalRecord = prevBest == null || session.ValidReps > prevBest.ValidReps;

            return Json(new
            {
                success = true,
                sessionId = session.Id,
                caloriesBurned = session.CaloriesBurned,
                isPersonalRecord
            });
        }

        // ────────────────────────────────────────────────────────────────
        // GET /AiWorkout/History  —  Lịch sử buổi tập
        // ────────────────────────────────────────────────────────────────
        public async Task<IActionResult> History(string exercise = "all", int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            const int pageSize = 10;

            var query = _db.WorkoutSessions
                .Where(w => w.MemberId == userId);

            if (exercise == "Squat" || exercise == "PushUp")
                query = query.Where(w => w.ExerciseType == exercise);

            var totalCount = await query.CountAsync();
            var sessions = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê tổng hợp
            var stats = await _db.WorkoutSessions
                .Where(w => w.MemberId == userId)
                .GroupBy(w => w.ExerciseType)
                .Select(g => new
                {
                    ExerciseType    = g.Key,
                    TotalSessions   = g.Count(),
                    TotalValidReps  = g.Sum(w => w.ValidReps),
                    TotalCalories   = g.Sum(w => w.CaloriesBurned),
                    AvgFormScore    = g.Average(w => w.AverageFormScore),
                    BestReps        = g.Max(w => w.ValidReps)
                })
                .ToListAsync();

            ViewBag.Sessions    = sessions;
            ViewBag.Stats       = stats;
            ViewBag.Exercise    = exercise;
            ViewBag.Page        = page;
            ViewBag.TotalPages  = (int)Math.Ceiling((double)totalCount / pageSize);

            return View();
        }
    }
}
