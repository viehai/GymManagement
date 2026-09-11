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
    /// Controller xử lý gửi, chỉnh sửa và xóa đánh giá phòng Gym từ phía hội viên.
    /// </summary>
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ReviewController(
            GymDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        // POST: /Review/Submit
        /// <summary>
        /// Tạo mới hoặc cập nhật đánh giá của hội viên cho phòng Gym.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(SubmitReviewInputModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMsg = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                               ?? "Dữ liệu đánh giá không hợp lệ.";
                TempData["Error"] = errorMsg;
                return RedirectToAction("Details", "Gym", new { id = model.GymId });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để đánh giá phòng gym.";
                return RedirectToAction("Details", "Gym", new { id = model.GymId });
            }

            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == model.GymId && g.Status == "Approved");
            if (gym == null)
            {
                TempData["Error"] = "Phòng Gym không tồn tại hoặc chưa được phê duyệt.";
                return RedirectToAction("Search", "Gym");
            }

            // 1. Kiểm tra điều kiện: Hội viên đã từng mua vé / có giao dịch thành công tại Gym này chưa
            bool hasPurchased = await _context.MemberMemberships
                .AnyAsync(m => m.MemberId == currentUser.Id && m.GymId == model.GymId);

            if (!hasPurchased)
            {
                hasPurchased = await _context.Transactions
                    .AnyAsync(t => t.MemberId == currentUser.Id && t.Status == "Success"
                                   && t.Membership != null && t.Membership.GymId == model.GymId);
            }

            if (!hasPurchased)
            {
                TempData["Error"] = "Bạn chỉ có thể đánh giá phòng Gym sau khi đã từng mua vé hoặc gói tập tại đây.";
                return RedirectToAction("Details", "Gym", new { id = model.GymId });
            }

            // 2. Bộ lọc từ ngữ cấm (Banned Words Filter)
            if (!string.IsNullOrWhiteSpace(model.Comment))
            {
                var bannedWords = await _context.BannedWords
                    .Select(b => b.Word)
                    .ToListAsync();

                // Quét cả danh sách cấu hình và danh sách từ cấm mặc định
                var allBannedWords = bannedWords.Union(ContentFilterHelper.DefaultBannedWords.Select(x => x.Word)).ToList();

                if (ContentFilterHelper.CheckForBannedWords(model.Comment, allBannedWords, out var matched))
                {
                    TempData["Error"] = $"Bình luận chứa từ ngữ không phù hợp quy chuẩn cộng đồng (\"{string.Join("\", \"", matched)}\"). Vui lòng chỉnh sửa lại từ ngữ lịch sự trước khi gửi!";
                    return RedirectToAction("Details", "Gym", new { id = model.GymId });
                }
            }

            // 3. Kiểm tra xem hội viên đã từng đánh giá phòng gym này chưa (1 Member - 1 Review / Gym)
            var existingReview = await _context.GymReviews
                .FirstOrDefaultAsync(r => r.GymId == model.GymId && r.MemberId == currentUser.Id);

            bool isUpdate = existingReview != null;

            if (isUpdate)
            {
                // Cập nhật đánh giá cũ
                existingReview!.Rating = model.Rating;
                existingReview.Comment = model.Comment?.Trim();
                existingReview.UpdatedAt = VnTime.Now;
                existingReview.IsVisible = true; // Cho phép hiển thị lại nếu trước đó bị ẩn và member đã sửa

                _context.GymReviews.Update(existingReview);
            }
            else
            {
                // Tạo mới đánh giá
                var newReview = new GymReview
                {
                    GymId = model.GymId,
                    MemberId = currentUser.Id,
                    Rating = model.Rating,
                    Comment = model.Comment?.Trim(),
                    CreatedAt = VnTime.Now,
                    IsVisible = true
                };

                _context.GymReviews.Add(newReview);
            }

            await _context.SaveChangesAsync();

            // 4. Bắn thông báo Real-time cho Chủ phòng Gym (Owner)
            try
            {
                var snippet = !string.IsNullOrWhiteSpace(model.Comment)
                    ? (model.Comment.Length > 50 ? model.Comment[..50] + "…" : model.Comment)
                    : "Không có nhận xét chữ";

                string notifTitle = isUpdate
                    ? $"Cập nhật đánh giá ({model.Rating}⭐) — {gym.Name}"
                    : $"Đánh giá mới ({model.Rating}⭐) — {gym.Name}";

                string notifMessage = $"Hội viên {currentUser.FullName} đã {(isUpdate ? "cập nhật" : "gửi")} đánh giá {model.Rating} sao: \"{snippet}\"";

                await NotificationHelper.CreateAsync(
                    _context,
                    userId: gym.OwnerId,
                    title: notifTitle,
                    message: notifMessage,
                    type: "Info",
                    category: "Review",
                    linkUrl: $"/OwnerReview/Index?gymId={gym.Id}",
                    hubContext: _hubContext);
            }
            catch
            {
                // Không chặn luồng chính nếu lỗi thông báo
            }

            TempData["Success"] = isUpdate
                ? "Cập nhật đánh giá của bạn thành công!"
                : "Cảm ơn bạn đã gửi đánh giá cho phòng Gym!";

            return Redirect($"/Gym/Details/{model.GymId}#reviews-section");
        }

        // POST: /Review/Delete/{id}
        /// <summary>
        /// Cho phép hội viên tự xóa đánh giá của chính mình.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            var review = await _context.GymReviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
            {
                TempData["Error"] = "Không tìm thấy đánh giá.";
                return RedirectToAction("Search", "Gym");
            }

            // Chỉ chủ sở hữu đánh giá mới được tự xóa ở action này
            if (review.MemberId != currentUser.Id && !User.IsInRole("Admin"))
            {
                TempData["Error"] = "Bạn không có quyền xóa đánh giá này.";
                return RedirectToAction("Details", "Gym", new { id = review.GymId });
            }

            int gymId = review.GymId;
            _context.GymReviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa đánh giá của bạn.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect($"/Gym/Details/{gymId}#reviews-section");
        }
    }
}
