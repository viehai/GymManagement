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
    /// Controller cho Chủ phòng Gym (Owner) quản lý các đánh giá, ẩn/hiện và phản hồi khách hàng.
    /// </summary>
    [Authorize(Roles = "Owner")]
    public class OwnerReviewController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        public OwnerReviewController(
            GymDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        // GET: /OwnerReview/Index?gymId=...&rating=...&isVisible=...
        /// <summary>
        /// Màn hình quản lý đánh giá theo cơ sở gym của Owner.
        /// </summary>
        public async Task<IActionResult> Index(int? gymId, int? rating, bool? isVisible)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Danh sách phòng Gym thuộc sở hữu của Owner
            var ownerGyms = await _context.Gyms
                .Where(g => g.OwnerId == currentUser.Id)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            if (!ownerGyms.Any())
            {
                ViewBag.NoGyms = true;
                return View(new OwnerReviewIndexViewModel());
            }

            // Chọn gym đang xét
            var selectedGym = ownerGyms.FirstOrDefault(g => g.Id == gymId) ?? ownerGyms.First();
            int currentGymId = selectedGym.Id;

            // Lấy toàn bộ review của gym này để tính toán thống kê
            var allGymReviews = await _context.GymReviews
                .Where(r => r.GymId == currentGymId)
                .ToListAsync();

            int totalCount = allGymReviews.Count;
            double avgRating = totalCount > 0 ? Math.Round(allGymReviews.Average(r => r.Rating), 1) : 0;
            int hiddenCount = allGymReviews.Count(r => !r.IsVisible);

            var starCounts = new Dictionary<int, int>
            {
                { 5, allGymReviews.Count(r => r.Rating == 5) },
                { 4, allGymReviews.Count(r => r.Rating == 4) },
                { 3, allGymReviews.Count(r => r.Rating == 3) },
                { 2, allGymReviews.Count(r => r.Rating == 2) },
                { 1, allGymReviews.Count(r => r.Rating == 1) }
            };

            // Lọc danh sách review theo tiêu chí
            var query = _context.GymReviews
                .Include(r => r.Member)
                .Include(r => r.Gym)
                .Where(r => r.GymId == currentGymId)
                .AsQueryable();

            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }

            if (isVisible.HasValue)
            {
                query = query.Where(r => r.IsVisible == isVisible.Value);
            }

            var reviewEntities = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var reviewVms = reviewEntities.Select(r => new GymReviewDisplayViewModel
            {
                Id = r.Id,
                GymId = r.GymId,
                GymName = r.Gym.Name,
                GymAddress = r.Gym.Address,
                MemberId = r.MemberId,
                MemberName = r.Member?.FullName ?? "Hội viên",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                IsVisible = r.IsVisible,
                OwnerReply = r.OwnerReply,
                OwnerRepliedAt = r.OwnerRepliedAt
            }).ToList();

            var vm = new OwnerReviewIndexViewModel
            {
                OwnerGyms = ownerGyms,
                SelectedGymId = currentGymId,
                SelectedGym = selectedGym,
                FilterRating = rating,
                FilterVisibility = isVisible,
                AverageRating = avgRating,
                TotalReviews = totalCount,
                HiddenReviewsCount = hiddenCount,
                StarCounts = starCounts,
                Reviews = reviewVms
            };

            return View(vm);
        }

        // POST: /OwnerReview/ToggleVisibility/{id}
        /// <summary>
        /// Chủ phòng gym ẩn hoặc cho hiển thị lại một đánh giá.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var review = await _context.GymReviews
                .Include(r => r.Gym)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null || review.Gym.OwnerId != currentUser.Id)
            {
                TempData["Error"] = "Không tìm thấy đánh giá hoặc bạn không có quyền thao tác.";
                return RedirectToAction(nameof(Index));
            }

            review.IsVisible = !review.IsVisible;
            await _context.SaveChangesAsync();

            TempData["Success"] = review.IsVisible
                ? "Đã cho hiển thị lại đánh giá này."
                : "Đã ẩn đánh giá khỏi trang công khai.";

            return RedirectToAction(nameof(Index), new { gymId = review.GymId });
        }

        // POST: /OwnerReview/Reply/{id}
        /// <summary>
        /// Chủ phòng gym viết hoặc chỉnh sửa phản hồi cho một đánh giá.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyText)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var review = await _context.GymReviews
                .Include(r => r.Gym)
                .Include(r => r.Member)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null || review.Gym.OwnerId != currentUser.Id)
            {
                TempData["Error"] = "Không tìm thấy đánh giá hoặc bạn không có quyền phản hồi.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(replyText))
            {
                TempData["Error"] = "Nội dung phản hồi không được để trống.";
                return RedirectToAction(nameof(Index), new { gymId = review.GymId });
            }

            // Quét từ cấm trong phản hồi của Owner
            var bannedWords = await _context.BannedWords.Select(b => b.Word).ToListAsync();
            var allBannedWords = bannedWords.Union(ContentFilterHelper.DefaultBannedWords.Select(x => x.Word)).ToList();

            if (ContentFilterHelper.CheckForBannedWords(replyText, allBannedWords, out var matched))
            {
                TempData["Error"] = $"Nội dung phản hồi chứa từ ngữ không phù hợp (\"{string.Join("\", \"", matched)}\"). Vui lòng kiểm tra lại!";
                return RedirectToAction(nameof(Index), new { gymId = review.GymId });
            }

            review.OwnerReply = replyText.Trim();
            review.OwnerRepliedAt = VnTime.Now;

            await _context.SaveChangesAsync();

            // Gửi thông báo Real-time cho Hội viên đã viết đánh giá
            try
            {
                var snippet = review.OwnerReply.Length > 50 ? review.OwnerReply[..50] + "…" : review.OwnerReply;

                await NotificationHelper.CreateAsync(
                    _context,
                    userId: review.MemberId,
                    title: $"Chủ phòng gym {review.Gym.Name} đã phản hồi đánh giá của bạn",
                    message: $"Phản hồi: \"{snippet}\"",
                    type: "Info",
                    category: "Review",
                    linkUrl: $"/Gym/Details/{review.GymId}#reviews-section",
                    hubContext: _hubContext);
            }
            catch
            {
                // Bỏ qua lỗi thông báo
            }

            TempData["Success"] = "Đã gửi phản hồi cho hội viên thành công!";
            return RedirectToAction(nameof(Index), new { gymId = review.GymId });
        }
    }
}
