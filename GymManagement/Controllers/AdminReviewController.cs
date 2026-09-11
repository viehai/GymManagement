using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Controller cho Quản trị viên (Admin) giám sát toàn bộ đánh giá trên sàn và cấu hình bộ lọc từ cấm.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminReviewController : Controller
    {
        private readonly GymDbContext _context;

        public AdminReviewController(GymDbContext context)
        {
            _context = context;
        }

        // GET: /AdminReview/Index
        /// <summary>
        /// Màn hình giám sát toàn bộ đánh giá toàn hệ thống.
        /// </summary>
        public async Task<IActionResult> Index(
            int? gymId,
            int? rating,
            bool? isVisible,
            string? search,
            int page = 1)
        {
            const int pageSize = 15;
            page = Math.Max(1, page);

            var allGyms = await _context.Gyms
                .OrderBy(g => g.Name)
                .ToListAsync();

            var totalReviewsCount = await _context.GymReviews.CountAsync();
            var hiddenReviewsCount = await _context.GymReviews.CountAsync(r => !r.IsVisible);
            var systemAverageRating = totalReviewsCount > 0
                ? Math.Round(await _context.GymReviews.AverageAsync(r => r.Rating), 1)
                : 0.0;

            var query = _context.GymReviews
                .Include(r => r.Gym)
                .Include(r => r.Member)
                .AsQueryable();

            if (gymId.HasValue)
            {
                query = query.Where(r => r.GymId == gymId.Value);
            }

            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }

            if (isVisible.HasValue)
            {
                query = query.Where(r => r.IsVisible == isVisible.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(r =>
                    (r.Comment != null && r.Comment.ToLower().Contains(kw)) ||
                    r.Gym.Name.ToLower().Contains(kw) ||
                    r.Member.FullName.ToLower().Contains(kw) ||
                    r.Member.Email!.ToLower().Contains(kw));
            }

            int totalItems = await query.CountAsync();

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new GymReviewDisplayViewModel
                {
                    Id = r.Id,
                    GymId = r.GymId,
                    GymName = r.Gym.Name,
                    GymAddress = r.Gym.Address,
                    MemberId = r.MemberId,
                    MemberName = r.Member.FullName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    IsVisible = r.IsVisible,
                    OwnerReply = r.OwnerReply,
                    OwnerRepliedAt = r.OwnerRepliedAt
                })
                .ToListAsync();

            var vm = new AdminReviewIndexViewModel
            {
                AllGyms = allGyms,
                FilterGymId = gymId,
                FilterRating = rating,
                FilterVisibility = isVisible,
                SearchKeyword = search,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                SystemAverageRating = systemAverageRating,
                TotalReviewsCount = totalReviewsCount,
                HiddenReviewsCount = hiddenReviewsCount,
                Reviews = reviews
            };

            return View(vm);
        }

        // POST: /AdminReview/ToggleVisibility/{id}
        /// <summary>
        /// Admin ẩn hoặc cho hiển thị lại một đánh giá.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(int id, string? returnUrl = null)
        {
            var review = await _context.GymReviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
            {
                TempData["Error"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            review.IsVisible = !review.IsVisible;
            await _context.SaveChangesAsync();

            TempData["Success"] = review.IsVisible
                ? "Đã cho hiển thị lại đánh giá này."
                : "Đã ẩn đánh giá khỏi trang người dùng.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminReview/Delete/{id}
        /// <summary>
        /// Admin xóa vĩnh viễn đánh giá vi phạm tiêu chuẩn cộng đồng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            var review = await _context.GymReviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
            {
                TempData["Error"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            _context.GymReviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa vĩnh viễn đánh giá.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminReview/BannedWords
        /// <summary>
        /// Màn hình thiết lập và quản lý danh sách từ ngữ cấm (Profanity Filter Setup).
        /// </summary>
        public async Task<IActionResult> BannedWords(string? category, string? search)
        {
            var query = _context.BannedWords.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(b => b.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(b => b.Word.ToLower().Contains(kw));
            }

            var words = await query
                .OrderBy(b => b.Category)
                .ThenBy(b => b.Word)
                .ToListAsync();

            var categories = await _context.BannedWords
                .Where(b => !string.IsNullOrEmpty(b.Category))
                .Select(b => b.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var vm = new BannedWordsManagementViewModel
            {
                BannedWords = words,
                SelectedCategory = category,
                SearchKeyword = search,
                Categories = categories
            };

            return View(vm);
        }

        // POST: /AdminReview/AddBannedWord
        /// <summary>
        /// Thêm từ ngữ cấm mới vào hệ thống.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBannedWord(string word, string? category)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                TempData["Error"] = "Vui lòng nhập từ ngữ muốn cấm.";
                return RedirectToAction(nameof(BannedWords));
            }

            var cleanWord = word.Trim().ToLower();
            bool exists = await _context.BannedWords.AnyAsync(b => b.Word.ToLower() == cleanWord);
            if (exists)
            {
                TempData["Warning"] = $"Từ ngữ \"{word.Trim()}\" đã tồn tại trong danh sách cấm.";
                return RedirectToAction(nameof(BannedWords));
            }

            var bannedWord = new BannedWord
            {
                Word = word.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "Thô tục" : category.Trim(),
                CreatedAt = VnTime.Now
            };

            _context.BannedWords.Add(bannedWord);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm từ ngữ \"{bannedWord.Word}\" vào danh mục cấm.";
            return RedirectToAction(nameof(BannedWords));
        }

        // POST: /AdminReview/DeleteBannedWord/{id}
        /// <summary>
        /// Xóa từ ngữ khỏi danh sách cấm.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBannedWord(int id)
        {
            var word = await _context.BannedWords.FindAsync(id);
            if (word == null)
            {
                TempData["Error"] = "Không tìm thấy từ ngữ cần xóa.";
                return RedirectToAction(nameof(BannedWords));
            }

            _context.BannedWords.Remove(word);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa từ \"{word.Word}\" khỏi danh sách cấm.";
            return RedirectToAction(nameof(BannedWords));
        }

        // POST: /AdminReview/SeedDefaultBannedWords
        /// <summary>
        /// Nạp nhanh bộ từ cấm mẫu mặc định vào cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedDefaultBannedWords()
        {
            int addedCount = 0;
            var existingWords = await _context.BannedWords
                .Select(b => b.Word.ToLower())
                .ToListAsync();

            foreach (var (word, cat) in ContentFilterHelper.DefaultBannedWords)
            {
                if (!existingWords.Contains(word.ToLower()))
                {
                    _context.BannedWords.Add(new BannedWord
                    {
                        Word = word,
                        Category = cat,
                        CreatedAt = VnTime.Now
                    });
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã nạp thành công {addedCount} từ ngữ cấm mẫu vào hệ thống!";
            }
            else
            {
                TempData["Warning"] = "Các từ ngữ cấm mẫu đều đã tồn tại trong danh sách.";
            }

            return RedirectToAction(nameof(BannedWords));
        }
    }
}
