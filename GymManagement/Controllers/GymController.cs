using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Controller xử lý trang Tìm kiếm và Chi tiết phòng Gym (công khai, không yêu cầu đăng nhập).
    /// </summary>
    public class GymController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GymController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Gym/Search?keyword=...
        /// <summary>Trang Tìm kiếm &amp; Danh sách phòng Gym đã được duyệt.</summary>
        public async Task<IActionResult> Search(string? keyword)
        {
            ViewData["Keyword"] = keyword ?? string.Empty;

            var query = _context.Gyms
                .Where(g => g.Status == "Approved")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(g =>
                    g.Name.ToLower().Contains(kw) ||
                    g.Address.ToLower().Contains(kw));
            }

            var gyms = await query
                .OrderByDescending(g => g.CreatedAt)
                .Select(g => new GymSearchViewModel
                {
                    Id          = g.Id,
                    Name        = g.Name,
                    Address     = g.Address,
                    Description = g.Description ?? string.Empty,
                    ImageUrl    = g.GymImages.Where(i => i.IsCover).Select(i => i.ImageUrl).FirstOrDefault()
                                  ?? g.GymImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault()
                                  ?? g.ImageUrl ?? string.Empty,
                    Status      = g.Status,
                    CreatedAt   = g.CreatedAt,
                    TotalReviews = g.GymReviews.Count(r => r.IsVisible),
                    AverageRating = g.GymReviews.Where(r => r.IsVisible).Any()
                        ? Math.Round(g.GymReviews.Where(r => r.IsVisible).Average(r => (double)r.Rating), 1)
                        : 0.0
                })
                .ToListAsync();

            return View(gyms);
        }

        // GET: /Gym/Details/{id}
        /// <summary>
        /// Trang chi tiết phòng Gym: thông tin tổng quan, thiết bị (IsVisible=true),
        /// gói vé (IsActive=true) và nút mua vé / đăng ký gói.
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            // Load gym + equipment (kèm catalog) + packages
            var gym = await _context.Gyms
                .Include(g => g.GymEquipments)
                    .ThenInclude(ge => ge.Equipment)   // catalog equipment
                .Include(g => g.MembershipPackages)
                .Include(g => g.GymImages)
                .FirstOrDefaultAsync(g => g.Id == id && g.Status == "Approved");

            if (gym == null)
                return NotFound();

            // ── Map Equipment: chỉ lấy IsVisible = true ──
            var equipments = gym.GymEquipments
                .Where(ge => ge.IsVisible)
                .Select(ge => new GymEquipmentDisplayViewModel
                {
                    IsCustom    = ge.IsCustom,
                    DisplayName = ge.IsCustom
                        ? (string.IsNullOrWhiteSpace(ge.CustomName) ? "Máy tập" : ge.CustomName)
                        : (ge.Equipment?.Name ?? "Máy tập"),
                    DisplayImage = ge.IsCustom
                        ? (ge.CustomImage ?? string.Empty)
                        : (ge.Equipment?.ImageUrl ?? string.Empty),
                    Category = ge.IsCustom
                        ? (!string.IsNullOrWhiteSpace(ge.CustomCategory) ? ge.CustomCategory : "Strength - Đa Dụng (Multi-purpose)")
                        : (!string.IsNullOrWhiteSpace(ge.Equipment?.Category) ? ge.Equipment.Category : "Khác")
                })
                .ToList();

            // ── Map Packages: chỉ lấy IsActive = true, sắp xếp theo Price tăng dần ──
            var packages = gym.MembershipPackages
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .Select(p => new PackageDisplayViewModel
                {
                    Id               = p.Id,
                    Name             = p.Name,
                    PackageType      = p.PackageType,
                    DurationInMonths = p.DurationInMonths,
                    Price            = p.Price
                })
                .ToList();

            var currentUser = await _userManager.GetUserAsync(User);
            bool isOwnerOfThisGym = currentUser != null && gym.OwnerId == currentUser.Id;

            // ── Map Gallery Images (V2) ──
            var galleryImages = gym.GymImages
                .OrderBy(gi => gi.DisplayOrder)
                .Select(gi => new GymImageViewModel
                {
                    Id           = gi.Id,
                    ImageUrl     = gi.ImageUrl,
                    DisplayOrder = gi.DisplayOrder,
                    IsCover      = gi.IsCover
                })
                .ToList();

            // ── Tính toán Thước đo độ đông đúc (Live Crowd Meter - V2) ──
            int maxCapacity = gym.MaxCapacity > 0 ? gym.MaxCapacity : 50;
            var twoHoursAgo = VnTime.Now.AddHours(-2);
            int activeCheckins = await _context.CheckinLogs
                .CountAsync(c => c.GymId == gym.Id && c.CheckinTime >= twoHoursAgo && c.CheckoutTime == null && c.Status == "Success");

            int crowdPct = Math.Min(100, (int)Math.Round((double)activeCheckins / maxCapacity * 100));
            var (cText, cColor, cIcon, cRec) = crowdPct switch
            {
                < 35 => ("Đang vắng", "#10b981", "bi-emoji-smile", "Phòng tập đang vắng, máy tập thoáng đãng — Thời điểm lý tưởng để tập luyện!"),
                <= 70 => ("Khá đông", "#f59e0b", "bi-people", "Phòng tập có lượng khách vừa phải, có thể cần chia sẻ máy tạ hoặc đổi bài linh hoạt."),
                _ => ("Rất đông / Giờ cao điểm", "#ef4444", "bi-exclamation-octagon", "Phòng tập đang trong giờ cao điểm — Nên cân nhắc đến vào khung giờ khác.")
            };

            // ── Rating & Review (V2) ──
            var visibleReviews = await _context.GymReviews
                .Include(r => r.Member)
                .Where(r => r.GymId == id && r.IsVisible)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            int totalReviews = visibleReviews.Count;
            double avgRating = totalReviews > 0 ? Math.Round(visibleReviews.Average(r => r.Rating), 1) : 0;

            var starCounts = new Dictionary<int, int>
            {
                { 5, visibleReviews.Count(r => r.Rating == 5) },
                { 4, visibleReviews.Count(r => r.Rating == 4) },
                { 3, visibleReviews.Count(r => r.Rating == 3) },
                { 2, visibleReviews.Count(r => r.Rating == 2) },
                { 1, visibleReviews.Count(r => r.Rating == 1) }
            };

            var reviewDisplays = visibleReviews.Select(r => new GymReviewDisplayViewModel
            {
                Id = r.Id,
                GymId = r.GymId,
                GymName = gym.Name,
                GymAddress = gym.Address,
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

            bool canReview = false;
            string? cannotReviewReason = null;
            GymReviewDisplayViewModel? currentUserReviewVm = null;

            if (currentUser != null)
            {
                var userReview = await _context.GymReviews
                    .FirstOrDefaultAsync(r => r.GymId == id && r.MemberId == currentUser.Id);

                if (userReview != null)
                {
                    currentUserReviewVm = new GymReviewDisplayViewModel
                    {
                        Id = userReview.Id,
                        GymId = userReview.GymId,
                        GymName = gym.Name,
                        GymAddress = gym.Address,
                        MemberId = userReview.MemberId,
                        MemberName = currentUser.FullName ?? "Hội viên",
                        Rating = userReview.Rating,
                        Comment = userReview.Comment,
                        CreatedAt = userReview.CreatedAt,
                        UpdatedAt = userReview.UpdatedAt,
                        IsVisible = userReview.IsVisible,
                        OwnerReply = userReview.OwnerReply,
                        OwnerRepliedAt = userReview.OwnerRepliedAt
                    };
                }

                bool hasPurchased = await _context.MemberMemberships
                    .AnyAsync(m => m.MemberId == currentUser.Id && m.GymId == id);

                if (!hasPurchased)
                {
                    hasPurchased = await _context.Transactions
                        .AnyAsync(t => t.MemberId == currentUser.Id && t.Status == "Success"
                                       && t.Membership != null && t.Membership.GymId == id);
                }

                if (hasPurchased)
                {
                    canReview = true;
                }
                else
                {
                    cannotReviewReason = "Chỉ hội viên đã từng mua vé hoặc gói tập tại cơ sở này mới có thể viết đánh giá.";
                }
            }
            else
            {
                cannotReviewReason = "Vui lòng đăng nhập tài khoản để viết đánh giá cho phòng Gym.";
            }

            var vm = new GymDetailsViewModel
            {
                Id                    = gym.Id,
                Name                  = gym.Name,
                Address               = gym.Address,
                Description           = gym.Description ?? string.Empty,
                ImageUrl              = gym.ImageUrl ?? string.Empty,
                OwnerId               = gym.OwnerId,
                IsOwnerOfThisGym      = isOwnerOfThisGym,
                Equipments            = equipments,
                Packages              = packages,
                GalleryImages         = galleryImages,
                MaxCapacity           = maxCapacity,
                CurrentActiveMembers  = activeCheckins,
                CrowdPercentage       = crowdPct,
                CrowdStatusText       = cText,
                CrowdStatusColor      = cColor,
                CrowdStatusIcon       = cIcon,
                CrowdRecommendation   = cRec,
                AverageRating         = avgRating,
                TotalReviews          = totalReviews,
                StarCounts            = starCounts,
                Reviews               = reviewDisplays,
                CanReview             = canReview,
                CannotReviewReason    = cannotReviewReason,
                CurrentUserReview     = currentUserReviewVm
            };

            return View(vm);
        }
    }
}

